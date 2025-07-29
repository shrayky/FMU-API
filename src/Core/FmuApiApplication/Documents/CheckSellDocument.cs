using CSharpFunctionalExtensions;
using FmuApiDomain.Cache.Interfaces;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Enums;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.State.Interfaces;
using FmuApiDomain.TrueApi.MarkData;
using FmuApiDomain.TrueApi.MarkData.Check;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Documents
{
    public class CheckSellDocument : IFrontolDocumentService
    {
        private RequestDocument _document { get; set; }
        private IMarkService _markInformationService { get; set; }
        private ICacheService _memcachedClient;
        IParametersService _parametersService { get; set; }
        private ILogger _logger { get; set; }
        private IApplicationState _appState { get;set; }

        private int _cacheExpirationMinutes = 30;
        private Parameters _configuration;

        private CheckSellDocument(
            RequestDocument requestDocument,
            IMarkService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService,
            IApplicationState applicationStateService,
            ILogger logger)
        {
            _document = requestDocument;
            _markInformationService = markInformationService;
            _logger = logger;
            _memcachedClient = cacheService;
            _parametersService = parametersService;
            _appState = applicationStateService;
            
            _configuration = _parametersService.Current();
        }

        private static CheckSellDocument CreateObject(
            RequestDocument requestDocument,
            IMarkService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService,
            IApplicationState applicationStateService,
            ILogger logger)
        {
            return new CheckSellDocument(requestDocument, markInformationService, cacheService, parametersService, applicationStateService, logger);
        }

        public static IFrontolDocumentService Create(
            RequestDocument requestDocument,
            IMarkService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService,
            IApplicationState applicationStateService,
            ILogger logger)
        {
            return CreateObject(requestDocument, markInformationService, cacheService, parametersService, applicationStateService, logger);
        }

        public async Task<Result<FmuAnswer>> ActionAsync()
        {
            var cachedAnswer = _memcachedClient.Get<FmuAnswer>(_document.Mark);

            if (cachedAnswer?.SGtin() == _document.Mark)
                return Result.Success(cachedAnswer);

            var checkResult = await MarkInformation();

            if (checkResult.IsSuccess)
                _memcachedClient.Set(checkResult.Value.SGtin(),
                                     checkResult.Value,
                                     TimeSpan.FromMinutes(_cacheExpirationMinutes));

            return checkResult;
        }

        private async Task<Result<FmuAnswer>> MarkInformation()
        {
            _logger.LogInformation("Марка для проверки {markCodeData}", _document.Mark);
            IMark mark = await _markInformationService.MarkAsync(_document.Mark);

            await SetOrganizationIdAsync(mark);
            var checkResult = await mark.PerformCheckAsync(OperationType.Sale);
            checkResult.Value.FillFieldsFor6255(_document.Inn);

            if (checkResult.IsSuccess)
            {
                return checkResult;
            }

            _logger.LogError(checkResult.Error);

            if (_configuration.SaleControlConfig.SendEmptyTrueApiAnswerWhenTimeoutError
                && _configuration.SaleControlConfig.RejectSalesWithoutCheckInformationFrom < DateTime.Now)
                return Result.Success(CreateFakeAnswer(mark, checkResult.Error));
            else
                return checkResult;
        }

        private async Task<Result<FmuAnswer>> MarkInformation(string markInBase64)
        {
            _logger.LogInformation("Марка для проверки {markCodeData}", markInBase64);

            IMark mark = await _markInformationService.MarkAsync(markInBase64);
            await SetOrganizationIdAsync(mark);

            var checkResult = await mark.PerformCheckAsync(OperationType.Sale);

            if (checkResult.IsSuccess)
                return checkResult;

            _logger.LogError(checkResult.Error);

            if (_configuration.SaleControlConfig.SendEmptyTrueApiAnswerWhenTimeoutError 
                && _configuration.SaleControlConfig.RejectSalesWithoutCheckInformationFrom <  DateTime.Now)
                return Result.Success(CreateFakeAnswer(mark, checkResult.Error));
            else
                return checkResult;
        }

        private FmuAnswer CreateFakeAnswer(IMark mark, string error)
        {
            var fakeCodeData = new CodeDataTrueApi
            {
                Cis = mark.Code,
                PrintView = mark.SGtin,
                Gtin = mark.Barcode,
                Valid = true,
                Verified = true,
                Found = true,
                Utilised = true,
                IsOwner = true,
                IsBlocked = false,
                IsTracking = false,
                Sold = false,
                Realizable = true,
                GrayZone = false,
            };

            var truemark_response = new CheckMarksDataTrueApi
            {
                Code = 0,
                Description = $"Ошибка проверки маркировки. {error}",
                ReqId = "00000000-0000-0000-0000-000000000000",
                ReqTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                Codes = [fakeCodeData]
            };

            _logger.LogWarning("[{Date}] - Ошибка проверки кода марки {Code}: {Error}",
                DateTime.Now, mark.Code, error);

            return new FmuAnswer
            {
                Code = 1,
                Error = error,
                Truemark_response = truemark_response
            };
        }

        private async Task SetOrganizationIdAsync(IMark mark)
        {
            if (_configuration.OrganisationConfig.PrintGroups.Count == 1)
                return;

            int pgCode = 0;
            string inn = _document.Inn;

            if (inn != string.Empty)
            {
                var organisation = _configuration.OrganisationConfig.PrintGroups.FirstOrDefault(p => p.INN == inn);

                if (organisation != null)
                    pgCode = organisation.Id;
            }
            else
                pgCode = await _markInformationService.WareSaleOrganizationFromFrontolBaseAsync(mark.Barcode);

            if (pgCode == 0)
                return;

            _logger.LogInformation("Код группы печати организации {pgCode}", pgCode);

            mark.SetPrintGroupCode(pgCode);
        }
    }
}
