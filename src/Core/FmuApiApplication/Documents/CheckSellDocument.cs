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
        private RequestDocument Document { get; set; }
        private ILogger<CheckSellDocument> Logger { get; set; }
        private Lazy<IFrontolSprTService> FrontolSprTSerice { get; set; }
        private Func<string, Task<IMark>> MarkFactory { get; set; }
        private readonly IMemoryCache _memcachedClient;
        IParametersService ParametersService { get; set; }
        private ICheckStatisticRepository CheckStatisticRepository { get; set; }

        private const int CacheExpirationMinutes = 30;
        private readonly Parameters _configuration;

        private CheckSellDocument(RequestDocument requestDocument, IServiceProvider provider)
        {
            Document = requestDocument;

            FrontolSprTSerice = new Lazy<IFrontolSprTService>(() => provider.GetRequiredService<IFrontolSprTService>());

            MarkFactory = provider.GetRequiredService<Func<string, Task<IMark>>>();

            CheckStatisticRepository = provider.GetRequiredService<ICheckStatisticRepository>();

            Logger = provider.GetRequiredService<ILogger<CheckSellDocument>>();
            _memcachedClient = provider.GetRequiredService<IMemoryCache>(); ;
            ParametersService = provider.GetRequiredService<IParametersService>();
            _configuration = ParametersService.Current();
        }

        private static CheckSellDocument CreateObject(RequestDocument requestDocument, IServiceProvider provider)
            => new(requestDocument, provider);

        public static IFrontolDocumentService Create(RequestDocument requestDocument, IServiceProvider provider)
            => CreateObject(requestDocument, provider);

        public async Task<Result<FmuAnswer>> ActionAsync()
        {
            var cachedAnswer = _memcachedClient.Get<FmuAnswer>(Document.Mark);

            if (cachedAnswer?.SGtin() == Document.Mark)
                return Result.Success(cachedAnswer);

            var checkResult = await MarkInformation();

            if (checkResult.IsSuccess)
                _memcachedClient.Set(checkResult.Value.SGtin(),
                                     checkResult.Value,
                                     TimeSpan.FromMinutes(CacheExpirationMinutes));
             
            return checkResult;
        }

        private async Task<Result<FmuAnswer>> MarkInformation()
        {
            Logger.LogInformation("Марка для проверки {markCodeData}", Document.Mark);
            var mark = await MarkFactory(Document.Mark);

            await SetOrganizationIdAsync(mark);

            var checkResult = await mark.PerformCheckAsync(OperationType.Sale);
            
            if (checkResult.IsSuccess)
            {
                checkResult.Value.FillFieldsFor6255(Document.Inn);

                if (checkResult.Value.Error == string.Empty && !checkResult.Value.OfflineRegime)
                    await CheckStatisticRepository.SuccessOnLineCheck(checkResult.Value.SGtin(), DateTime.Now);
                else if (checkResult.Value.Error == string.Empty && checkResult.Value.OfflineRegime)
                    await CheckStatisticRepository.SuccessOffLineCheck(checkResult.Value.SGtin(), DateTime.Now);
                else if (checkResult.Value.Error != string.Empty && !checkResult.Value.OfflineRegime)
                    await CheckStatisticRepository.OnLineCheckWithWarnings(checkResult.Value.SGtin(), DateTime.Now, checkResult.Value.Error);
                else if (checkResult.Value.Error != string.Empty && checkResult.Value.OfflineRegime)
                    await CheckStatisticRepository.OffLineCheckWithWarnings(checkResult.Value.SGtin(), DateTime.Now, checkResult.Value.Error);

                return checkResult;
            }
            else
            {
                await CheckStatisticRepository.FailureCheck(mark.SGtin, DateTime.Now);
            }

            Logger.LogError(checkResult.Error);

            if (_configuration.SaleControlConfig.SendEmptyTrueApiAnswerWhenTimeoutError
                && _configuration.SaleControlConfig.RejectSalesWithoutCheckInformationFrom < DateTime.Now)
                return Result.Success(CreateFakeAnswer(mark, checkResult.Error));
            else
                return checkResult;
        }

        private async Task<Result<FmuAnswer>> MarkInformation(string markInBase64)
        {
            Logger.LogInformation("Марка для проверки {markCodeData}", markInBase64);

            IMark mark = await MarkFactory(markInBase64);
            await SetOrganizationIdAsync(mark);

            var checkResult = await mark.PerformCheckAsync(OperationType.Sale);

            if (checkResult.IsSuccess)
                return checkResult;

            Logger.LogError(checkResult.Error);

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

            Logger.LogWarning("[{Date}] - Ошибка проверки кода марки {Code}: {Error}",
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
            string inn = Document.Inn;

            if (inn != string.Empty)
            {
                var organisation = _configuration.OrganisationConfig.PrintGroups.FirstOrDefault(p => p.INN == inn);

                if (organisation != null)
                    pgCode = organisation.Id;
            }
            else
            {
                var result = await FrontolSprTSerice.Value.PrintGroupCodeByBarcodeAsync(mark.Barcode);

                if (result.IsSuccess)
                    pgCode = result.Value;
            }

            if (pgCode == 0)
                return;

            Logger.LogInformation("Код группы печати организации {pgCode}", pgCode);

            mark.SetPrintGroupCode(pgCode);
        }
    }
}
