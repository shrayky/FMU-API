using CSharpFunctionalExtensions;
using FmuApiApplication.Mark.Interfaces;
using FmuApiApplication.Services.TrueSign;
using FmuApiDomain.Configuration;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.TrueApi.MarkData.Check;
using Microsoft.Extensions.Logging;
using FmuApiDomain.TrueApi.MarkData;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.MarkInformation.Entities;
using FmuApiDomain.State.Interfaces;
using FmuApiDomain.MarkInformation.Enums;
using FmuApiApplication.Mark.Models;
using LocalModuleIntegration.Interfaces;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.LocalModule.Enums;

namespace FmuApiApplication.Mark.Services
{
    public class MarkChecker : IMarkChecker
    {
        private readonly ILogger<MarkChecker> _logger;
        private readonly IParametersService _parametersService;
        private readonly MarksCheckService _trueApiCheck;
        private readonly Parameters _configuration;
        private readonly IApplicationState _applicationState;
        private readonly ILocalModuleService _localModuleService;

        public MarkChecker(ILogger<MarkChecker> logger,
            IParametersService parametersService,
            MarksCheckService trueApiCheck,
            IApplicationState applicationState,
            ILocalModuleService localModuleService)
        {
            _logger = logger;
            _parametersService = parametersService;
            _trueApiCheck = trueApiCheck;
            _configuration = _parametersService.Current();
            _applicationState = applicationState;
            _localModuleService = localModuleService;
        }

        public async Task<MarkCheckResult> FmuApiDatabaseCheck(string sgtin, IMarkStateManager stateManager)
        {
            _logger.LogInformation("Начало database проверки марки {Sgtin}", sgtin);

            if (!_configuration.Database.OfflineCheckIsEnabled)
            {
                _logger.LogInformation("Database проверка отключена");
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

            if (!_applicationState.IsOnline())
                return MarkCheckResult.Failure("Нет интернета");

            if (_applicationState.WithoutOnlineCheck())
                return MarkCheckResult.Failure("Онлайн проверка отключена");

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

            var result = MarkCheckResult.FromCheck(trueMarkCheckResult);

            var markInfo = new MarkEntity()
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

        private bool ValidateOnlineCheckPossibility(bool codeIsSgtin)
        {
            return !codeIsSgtin || _applicationState.IsOnline();
        }

        private static CheckMarksDataTrueApi CreateTrueMarkDataFromInfo(FmuApiDomain.MarkInformation.Entities.MarkEntity markInfo)
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

        public async Task<MarkCheckResult> OfflineCheckAsync(string cis, int organizationId)
        {
            string xApiKey = _configuration.OrganisationConfig.XapiKey(organizationId);

            if (string.IsNullOrEmpty(xApiKey))
                return MarkCheckResult.Failure($"Не получен XAPIKEY для организации с кодом {organizationId}, offline проверка {cis} невозможна.");

            LocalModuleConnection connection = _configuration.OrganisationConfig.LocalModuleConnection(organizationId);

            if (!connection.Enable)
                return MarkCheckResult.Failure($"Локальный модуль отключен для организации с кодом {organizationId}, offline проверка {cis} невозможна.");

            var lmState = _applicationState.OrganizationLocalModuleStatus(organizationId);

            if (lmState != LocalModuleStatus.Ready)
                return MarkCheckResult.Failure($"Локальный модуль для организации с кодом {organizationId} находится в состоянии {lmState}, offline проверка {cis} невозможна.");

            Result<CheckMarksDataTrueApi> trueMarkCheckResult;

            try
            {
                trueMarkCheckResult = await _localModuleService.OutCheckAsync(connection, cis, xApiKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при offline проверке марки {Code}", cis);
                return MarkCheckResult.Failure($"Ошибка offline проверки: {ex.Message}");
            }

            if (trueMarkCheckResult.IsFailure)
            {
                _logger.LogWarning("Ошибка проверки марки {Code}: {Error}", cis, trueMarkCheckResult.Error);
                return MarkCheckResult.Failure(trueMarkCheckResult.Error);
            }

            var markData = trueMarkCheckResult.Value.MarkData();
            if (markData.Empty)
            {
                return MarkCheckResult.Failure($"Пустой результат проверки по коду марки {cis}");
            }

            markData.Found = true;
            markData.Valid = true;
            markData.Utilised = true;
            markData.IsOwner = true;
            markData.Verified = true;
            markData.Realizable = true;

            var result = MarkCheckResult.FromCheck(trueMarkCheckResult);

            result.FmuAnswer.OfflineRegime = true;

            var markInfo = new MarkEntity()
            {
                MarkId = cis,
                State = markData.Sold ? MarkState.Sold : MarkState.Stock,
                TrueApiCisData = markData,
                TrueApiAnswerProperties = new()
                {
                    Code = trueMarkCheckResult.Value.Code,
                    Description = trueMarkCheckResult.Value.Description,
                    ReqId = _configuration.SaleControlConfig.SendLocalModuleInformationalInRequestId ? 
                                                             trueMarkCheckResult.Value.ReqId : 
                                                             $"{trueMarkCheckResult.Value.ReqId}&{trueMarkCheckResult.Value.Inst}&{trueMarkCheckResult.Value.Version}",
                    ReqTimestamp = trueMarkCheckResult.Value.ReqTimestamp,
                    Inst = trueMarkCheckResult.Value.Inst,
                    Version = trueMarkCheckResult.Value.Version
                },
            };

            if (_configuration.SaleControlConfig.SendLocalModuleInformationalInRequestId)
                result.TrueMarkData.ReqId = $"{trueMarkCheckResult.Value.ReqId}&Inst{trueMarkCheckResult.Value.Inst}&Ver{trueMarkCheckResult.Value.Version}";
            else
                result.TrueMarkData.ReqId = trueMarkCheckResult.Value.ReqId;

            result.SetMarkInformation(markInfo);

            return result;
        }
    }
}
