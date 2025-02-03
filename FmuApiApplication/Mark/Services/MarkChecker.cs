using CSharpFunctionalExtensions;
using FmuApiApplication.Mark.Interfaces;
using FmuApiApplication.Services.TrueSign;
using FmuApiDomain.Configuration;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.MarkInformation;
using FmuApiDomain.TrueSignApi.MarkData.Check;
using FmuApiDomain.TrueSignApi.MarkData;
using Microsoft.Extensions.Logging;
using FmuApiApplication.Services.TrueSign.Models;
using FmuApiSettings;
using FmuApiDomain.Frontol;

namespace FmuApiApplication.Mark.Services
{
    public class MarkChecker : IMarkChecker
    {
        private readonly ILogger<MarkChecker> _logger;
        private readonly IParametersService _parametersService;
        private readonly MarksCheckService _trueApiCheck;
        private readonly Parameters _configuration;

        public MarkChecker(
            ILogger<MarkChecker> logger,
            IParametersService parametersService,
            MarksCheckService trueApiCheck)
        {
            _logger = logger;
            _parametersService = parametersService;
            _trueApiCheck = trueApiCheck;
            _configuration = _parametersService.Current();
        }
        public async Task<MarkCheckResult> OfflineCheck(string sgtin, IMarkStateManager stateManager)
        {
            _logger.LogInformation("Начало offline проверки марки {Sgtin}", sgtin);

            if (!_configuration.Database.OfflineCheckIsEnabled)
            {
                _logger.LogInformation("Offline проверка отключена");
                return MarkCheckResult.Success(new(), new(), new());
            }

            var markInfo = await stateManager.GetMarkInformation(sgtin);

            if (!markInfo.HaveTrueApiAnswer)
            {
                _logger.LogInformation("Нет данных TrueApi для марки {Sgtin}", sgtin);
                return MarkCheckResult.Success(new(), markInfo, new());
            }

            var trueMarkData = CreateTrueMarkDataFromInfo(markInfo);
            var fmuAnswer = CreateFmuAnswer(trueMarkData);

            fmuAnswer.Offline = true;

            return MarkCheckResult.Success(trueMarkData, markInfo, fmuAnswer);

        }

        public async Task<MarkCheckResult> OnlineCheck(string code, string sgtin, bool codeIsSgtin, int printGroupCode)
        {
            _logger.LogInformation("Начало online проверки марки {Code}", code);

            if (!ValidateOnlineCheckPossibility(codeIsSgtin))
                return MarkCheckResult.Failure("Онлайн проверка по неполному коду невозможна!");

            if (!Constants.Online)
                return MarkCheckResult.Failure("Нет интернета");

            Result<CheckMarksDataTrueApi> trueMarkCheckResult;

            try
            {
                var checkMarksRequestData = new CheckMarksRequestData(code);
                trueMarkCheckResult = await _trueApiCheck.RequestMarkState(checkMarksRequestData, printGroupCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при online проверке марки {Code}", code);
                return MarkCheckResult.Failure($"Ошибка online проверки: {ex.Message}");
            }

            if (trueMarkCheckResult.IsFailure)
            {
                _logger.LogWarning("Ошибка проверки марки {Code}: {Error}", code, trueMarkCheckResult.Error);
                return MarkCheckResult.Failure(trueMarkCheckResult.Error);
            }

            var markData = trueMarkCheckResult.Value.MarkData();
            if (markData.Empty)
            {
                return MarkCheckResult.Failure($"Пустой результат проверки по коду марки {code}");
            }

            var result = MarkCheckResult.FromOnlineCheck(trueMarkCheckResult);

            // Создаем и устанавливаем информацию о марке через метод
            var markInfo = new MarkInformation
            {
                MarkId = sgtin,
                State = markData.Sold ? MarkState.Sold : MarkState.Stock,
                TrueApiCisData = markData,
                TrueApiAnswerProperties = new()
                {
                    Code = trueMarkCheckResult.Value.Code,
                    Description = trueMarkCheckResult.Value.Description,
                    ReqId = trueMarkCheckResult.Value.ReqId,
                    ReqTimestamp = trueMarkCheckResult.Value.ReqTimestamp
                }
            };

            result.SetMarkInformation(markInfo);
            
            return result;
        }

        private bool ValidateOnlineCheckPossibility(bool codeisSgtin)
        {
            return !codeisSgtin || Constants.Online;
        }

        private bool IsReturnedAndBanned(MarkInformation markInfo)
        {
            return markInfo.State == MarkState.Returned &&
                   _configuration.SaleControlConfig.BanSalesReturnedWares;
        }

        private static CheckMarksDataTrueApi CreateTrueMarkDataFromInfo(MarkInformation markInfo)
        {
            markInfo.TrueApiCisData.Sold = markInfo.IsSold;

            return new CheckMarksDataTrueApi
            {
                Code = markInfo.TrueApiAnswerProperties.Code,
                Description = markInfo.TrueApiAnswerProperties.Description,
                ReqId = markInfo.TrueApiAnswerProperties.ReqId,
                ReqTimestamp = markInfo.TrueApiAnswerProperties.ReqTimestamp,
                Codes = new List<CodeDataTrueApi> { markInfo.TrueApiCisData }
            };
        }

        private static FmuAnswer CreateFmuAnswer(CheckMarksDataTrueApi trueMarkData)
        {
            return new FmuAnswer
            {
                Code = 0,
                Error = "Данные получены в offline режиме",
                Truemark_response = trueMarkData
            };
        }
    }
}
