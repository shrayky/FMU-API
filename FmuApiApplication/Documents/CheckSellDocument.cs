using CSharpFunctionalExtensions;
using FmuApiDomain.Cache;
using FmuApiDomain.Configuration;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.MarkInformation;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.TrueSignApi.MarkData;
using FmuApiDomain.TrueSignApi.MarkData.Check;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Documents
{
    public class CheckSellDocument : IFrontolDocumentService
    {
        private RequestDocument _document { get; set; }
        private IMarkInformationService _markInformationService { get; set; }
        private ICacheService _memcachedClient;
        IParametersService _parametersService { get; set; }
        private ILogger _logger { get; set; }

        private int _cacheExpirationMinutes = 30;
        private Parameters _configuration;

        private CheckSellDocument(
            RequestDocument requestDocument,
            IMarkInformationService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService, 
            ILogger logger)
        {
            _document = requestDocument;
            _markInformationService = markInformationService;
            _logger = logger;
            _memcachedClient = cacheService;
            _parametersService = parametersService;

            _configuration = _parametersService.Current();
        }

        private static CheckSellDocument CreateObject(
            RequestDocument requestDocument,
            IMarkInformationService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService, 
            ILogger logger)
        {
            return new CheckSellDocument(requestDocument, markInformationService, cacheService, parametersService, logger);
        }

        public static IFrontolDocumentService Create(
            RequestDocument requestDocument,
            IMarkInformationService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService, 
            ILogger logger)
        {
            return CreateObject(requestDocument, markInformationService, cacheService, parametersService, logger);
        }

        public async Task<Result<FmuAnswer>> ActionAsync()
        {
            var cachedAnswer = _memcachedClient.Get<FmuAnswer>(_document.Mark());

            if (cachedAnswer?.SGtin() == _document.Mark())
                return Result.Success(cachedAnswer);

            var checkResult = await MarkInformation(_document.Mark());

            if (checkResult.IsSuccess)
                _memcachedClient.Set(checkResult.Value.SGtin(),
                                     checkResult.Value,
                                     TimeSpan.FromMinutes(_cacheExpirationMinutes));

            return checkResult;
        }

        private async Task<Result<FmuAnswer>> MarkInformation(string markInBase64)
        {
            _logger.LogInformation("Марка для проверки {markCodeData}", markInBase64);

            IMark mark = await _markInformationService.MarkAsync(markInBase64);
            await SetOrganiztaionIdAsync(mark);

            var checkResult = await mark.PerformCheckAsync();

            if (!checkResult.IsSuccess)
            {
                // Если настроено не отправлять пустой ответ при ошибке
                if (!_configuration.SaleControlConfig.SendEmptyTrueApiAnswerWhenTimeoutError)
                {
                    return checkResult;
                }

                // Создаем фейковый ответ при ошибке
                return Result.Success(CreateFakeAnswer(mark, checkResult.Error));
            }

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

        private async Task SetOrganiztaionIdAsync(IMark mark)
        {
            if (_configuration.OrganisationConfig.PrintGroups.Count == 1)
                return;

            int pgCode = await _markInformationService.WareSaleOrganizationFromFrontolBaseAsync(mark.Barcode);

            if (pgCode == 0)
                return;

            _logger.LogInformation("Код группы печати организации {pgCode}", pgCode);

            mark.SetPrintGroupCode(pgCode);
        }
    }
}
