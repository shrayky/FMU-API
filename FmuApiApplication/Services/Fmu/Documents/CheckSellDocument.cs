using CSharpFunctionalExtensions;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.MarkInformation;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.TrueSignApi.MarkData;
using FmuApiDomain.TrueSignApi.MarkData.Check;
using FmuApiSettings;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Services.Fmu.Documents
{
    public class CheckSellDocument : IFrontolDocumentService
    {
        private RequestDocument _document { get; set; }
        private IMarkInformationService _markInformationService { get; set; }
        private ILogger _logger { get; set; }

        private CheckSellDocument(RequestDocument requestDocument, IMarkInformationService markInformationService, ILogger logger)
        {
            _document = requestDocument;
            _markInformationService = markInformationService;
            _logger = logger;
        }

        private static CheckSellDocument CreateObjext(RequestDocument requestDocument, IMarkInformationService markInformationService, ILogger logger)
        {
            return new CheckSellDocument(requestDocument, markInformationService, logger);
        }

        public static IFrontolDocumentService Create(RequestDocument requestDocument, IMarkInformationService markInformationService, ILogger logger)
        {
            return CreateObjext(requestDocument, markInformationService, logger);
        }
        public async Task<Result<FmuAnswer>> ActionAsync()
        {
            if (Constants.LastCheckMarkInformation.SGtin() == _document.Mark())
                return Result.Success(Constants.LastCheckMarkInformation);

            var checkResult =  await MarkInformation(_document.Mark());

            if (checkResult.IsSuccess)
                Constants.LastCheckMarkInformation = checkResult.Value;

            return checkResult;
        }

        private async Task<Result<FmuAnswer>> MarkInformation(string markInbase64)
        {
            FmuAnswer answer;

            _logger.LogInformation("Марка для проверки {markCodeData}", markInbase64);

            IMark mark = await _markInformationService.MarkAsync(markInbase64);

            await SetOrganistaionIdAsync(mark);

            var offlineCheckResult = await mark.OfflineCheckAsync();

            if (offlineCheckResult.IsFailure)
            {
                _logger.LogWarning("[{Date}] - Ошибка оффлайн проверки кода марки {Code}: {Err}", DateTime.Now, mark.Code, offlineCheckResult.Error);
                return Result.Success(mark.MarkDataAfterCheck());
            }

            bool markIsAlredySold = mark.CodeIsSgtin & mark.DatabaseState().HaveTrueApiAnswer & mark.DatabaseState().State == MarkState.Sold;

            if (markIsAlredySold)
                return Result.Success(mark.MarkDataAfterCheck());

            CheckMarksDataTrueApi trueApiDataAboutMark = mark.TrueApiData();

            var onlineCheckResult = await mark.OnlineCheckAsync();

            CheckMarksDataTrueApi Truemark_response;

            if (onlineCheckResult.IsFailure && trueApiDataAboutMark.Codes.Count > 0)
                return Result.Success(mark.MarkDataAfterCheck());

            if (onlineCheckResult.IsFailure && !Constants.Parametrs.SaleControlConfig.SendEmptyTrueApiAnswerWhenTimeoutError)
            {
                return Result.Failure<FmuAnswer>(onlineCheckResult.Error);
            }

            if (onlineCheckResult.IsFailure)
            {
                CodeDataTrueApi fakeCodeData = new()
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

                Truemark_response = new()
                {
                    Code = 0,
                    Description = $"Ошибка проверки маркировки. {onlineCheckResult.Error}",
                    ReqId = "00000000-0000-0000-0000-000000000000",
                    ReqTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                    Codes = [fakeCodeData]
                };

                _logger.LogWarning("[{Date}] - Ошибка онлайн проверки кода марки {Code}: {Err}", DateTime.Now, mark.Code, onlineCheckResult.Error);
            }
            else
                Truemark_response = mark.TrueApiData();

            await mark.SaveAsync();

            answer = new()
            {
                Code = mark.ErrorDescription == string.Empty ? 0 : 1,
                Error = mark.ErrorDescription,
                Truemark_response = Truemark_response,
            };

            return Result.Success(answer);
        }

        private async Task SetOrganistaionIdAsync(IMark mark)
        {
            if (Constants.Parametrs.OrganisationConfig.PrintGroups.Count == 1)
                return;

            int pgCode = await _markInformationService.WareSaleOrganisationFromFrontolBaseAsync(mark.Barcode);

            if (pgCode == 0)
                return;

            _logger.LogInformation("Код группы печати организации {pgCode}", pgCode);

            mark.SetPrintGroupCode(pgCode);
        }
    }
}
