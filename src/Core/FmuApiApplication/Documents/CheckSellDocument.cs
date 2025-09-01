using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Enums;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.Frontol.Interfaces;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.Repositories;
using FmuApiDomain.TrueApi.MarkData;
using FmuApiDomain.TrueApi.MarkData.Check;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Documents
{
    public class CheckSellDocument : IFrontolDocumentService
    {
        private RequestDocument _document { get; set; }
        private ILogger<CheckSellDocument> _logger { get; set; }
        private Lazy<IFrontolSprTService> _frontolSprTSerice { get; set; }
        private Func<string, Task<IMark>> _markFactory { get; set; }
        private IMemoryCache _memcachedClient;
        IParametersService _parametersService { get; set; }
        private ICheckStatisticRepository _checkStatisticRepository { get; set; }

        private int _cacheExpirationMinutes = 30;
        private Parameters _configuration;

        private CheckSellDocument(RequestDocument requestDocument, IServiceProvider provider)
        {
            _document = requestDocument;

            _frontolSprTSerice = new Lazy<IFrontolSprTService>(() => provider.GetRequiredService<IFrontolSprTService>());

            _markFactory = provider.GetRequiredService<Func<string, Task<IMark>>>();

            _checkStatisticRepository = provider.GetRequiredService<ICheckStatisticRepository>();

            _logger = provider.GetRequiredService<ILogger<CheckSellDocument>>();
            _memcachedClient = provider.GetRequiredService<IMemoryCache>(); ;
            _parametersService = provider.GetRequiredService<IParametersService>();
            _configuration = _parametersService.Current();
        }

        private static CheckSellDocument CreateObject(RequestDocument requestDocument, IServiceProvider provider)
            => new(requestDocument, provider);

        public static IFrontolDocumentService Create(RequestDocument requestDocument, IServiceProvider provider)
            => CreateObject(requestDocument, provider);

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
            IMark mark = await _markFactory(_document.Mark);

            await SetOrganizationIdAsync(mark);

            var checkResult = await mark.PerformCheckAsync(OperationType.Sale);
            
            if (checkResult.IsSuccess)
            {
                checkResult.Value.FillFieldsFor6255(_document.Inn);

                if (checkResult.Value.Error == string.Empty && !checkResult.Value.OfflineRegime)
                    await _checkStatisticRepository.SuccessOnLineCheck(checkResult.Value.SGtin(), DateTime.Now);
                else if (checkResult.Value.Error == string.Empty && checkResult.Value.OfflineRegime)
                    await _checkStatisticRepository.SuccessOffLineCheck(checkResult.Value.SGtin(), DateTime.Now);
                else if (checkResult.Value.Error != string.Empty && !checkResult.Value.OfflineRegime)
                    await _checkStatisticRepository.OnLineCheckWithWarnings(checkResult.Value.SGtin(), DateTime.Now, checkResult.Value.Error);
                else if (checkResult.Value.Error != string.Empty && checkResult.Value.OfflineRegime)
                    await _checkStatisticRepository.OffLineCheckWithWarnings(checkResult.Value.SGtin(), DateTime.Now, checkResult.Value.Error);

                return checkResult;
            }
            else
            {
                await _checkStatisticRepository.FailureCheck(mark.SGtin, DateTime.Now);
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

            IMark mark = await _markFactory(markInBase64);
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
            {
                var result = await _frontolSprTSerice.Value.PrintGroupCodeByBarcodeAsync(mark.Barcode);

                if (result.IsSuccess)
                    pgCode = result.Value;
            }

            if (pgCode == 0)
                return;

            _logger.LogInformation("Код группы печати организации {pgCode}", pgCode);

            mark.SetPrintGroupCode(pgCode);
        }
    }
}
