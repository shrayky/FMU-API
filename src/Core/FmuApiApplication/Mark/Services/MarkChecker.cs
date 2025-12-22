using CSharpFunctionalExtensions;
using FmuApiApplication.Mark.Interfaces;
using FmuApiApplication.Mark.Models;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.LocalModule.Enums;
using FmuApiDomain.MarkInformation.Entities;
using FmuApiDomain.MarkInformation.Enums;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.State.Interfaces;
using FmuApiDomain.TrueApi.Interfaces;
using FmuApiDomain.TrueApi.MarkData;
using FmuApiDomain.TrueApi.MarkData.Check;
using LocalModuleIntegration.Interfaces;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Mark.Services
{
    public class MarkChecker : IMarkChecker
    {
        private readonly ILogger<MarkChecker> _logger;
        private readonly IParametersService _parametersService;
        private readonly IOnLineMarkCheckService _onlineMarkCHeck;
        private readonly ILocalModuleService _localModuleService;
        private readonly IApplicationState _applicationState;

        private readonly Parameters _configuration;

        public MarkChecker(ILogger<MarkChecker> logger,
            IParametersService parametersService,
            IOnLineMarkCheckService onlineMarkCheck,
            IApplicationState applicationState,
            ILocalModuleService localModuleService)
        {
            _logger = logger;
            _parametersService = parametersService;
            _onlineMarkCHeck = onlineMarkCheck;
            _configuration = _parametersService.Current();
            _applicationState = applicationState;
            _localModuleService = localModuleService;
        }

        public async Task<MarkCheckResult> FmuApiDatabaseCheck(string sgtin, IMarkStateManager stateManager)
        {
            _logger.LogInformation("Начало database проверки марки {Sgtin}", sgtin);

            if (!_configuration.Database.DatabaseCheckIsEnabled)
            {
                _logger.LogInformation("Database проверка отключена");
                return MarkCheckResult.Success(new(), new(), new());
            }

            var markInfo = await stateManager.Information(sgtin);

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
                trueMarkCheckResult = await _onlineMarkCHeck.RequestMarkState(checkMarksRequestData, printGroupCode);
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
            _logger.LogWarning("Производится проверка марки {сis} в локальном модуле", cis);

            var xApiKey = _configuration.OrganisationConfig.XapiKey(organizationId);

            if (string.IsNullOrEmpty(xApiKey))
                return MarkCheckResult.Failure($"Не получен XAPIKEY для организации с кодом {organizationId}, off-line проверка {cis} невозможна.");

            var connection = _configuration.OrganisationConfig.LocalModuleConnection(organizationId);

            if (!connection.Enable)
                return MarkCheckResult.Failure($"Локальный модуль отключен для организации с кодом {organizationId}, off-line проверка {cis} невозможна.");

            var lmState = _applicationState.OrganizationLocalModuleStatus(organizationId);

            if (lmState != LocalModuleStatus.Ready)
                return MarkCheckResult.Failure($"Локальный модуль для организации с кодом {organizationId} находится в состоянии {lmState}, off-line проверка {cis} невозможна.");

            Result<CheckMarksDataTrueApi> trueMarkCheckResult;

            try
            {
                trueMarkCheckResult = await _localModuleService.OutCheckAsync(connection, cis, xApiKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при off-line проверке марки {Code}", cis);
                return MarkCheckResult.Failure($"Ошибка off-line проверки: {ex.Message}");
            }

            if (trueMarkCheckResult.IsFailure)
            {
                _logger.LogWarning("Ошибка проверки марки {Code}: {Error}", cis, trueMarkCheckResult.Error);
                return MarkCheckResult.Failure(trueMarkCheckResult.Error);
            }

            var markData = trueMarkCheckResult.Value.MarkData();
            
            if (markData.Empty)
                return MarkCheckResult.Failure($"Пустой результат оффлайн проверки по коду марки {cis}");

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
                    ReqId = trueMarkCheckResult.Value.ReqId,
                    ReqTimestamp = trueMarkCheckResult.Value.ReqTimestamp,
                    Inst = trueMarkCheckResult.Value.Inst,
                    Version = trueMarkCheckResult.Value.Version
                },
            };

            result.TrueMarkData.ReqId = _configuration.SaleControlConfig.SendLocalModuleInformationalInRequestId ?
                $"{trueMarkCheckResult.Value.ReqId}&Inst={trueMarkCheckResult.Value.Inst}&Ver={trueMarkCheckResult.Value.Version}"
                : trueMarkCheckResult.Value.ReqId;

           result.SetMarkInformation(markInfo);

           return result;
        }

        public async Task<MarkCheckResult> TsPiotCheck(string code, TsPiotConnectionSettings tsPiotConnectionSettings)
        {
            throw new NotImplementedException();
        }
    }
}
