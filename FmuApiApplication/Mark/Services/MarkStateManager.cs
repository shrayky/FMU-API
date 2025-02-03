using CouchDb.Handlers;
using CSharpFunctionalExtensions;
using FmuApiApplication.Mark.Interfaces;
using FmuApiDomain.Configuration;
using FmuApiDomain.MarkInformation;
using FmuApiDomain.TrueSignApi.MarkData.Check;
using FmuApiDomain.TrueSignApi.MarkData;
using Interfaces;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Mark.Services
{
    public class MarkStateManager : IMarkStateManager
    {
        private readonly MarkInformationHandler _markStateCrud;
        private readonly IParametersService _parametersService;
        private readonly ILogger<MarkStateManager> _logger;
        private readonly Parameters _configuration;

        public MarkStateManager(
            MarkInformationHandler markStateCrud,
            IParametersService parametersService,
            ILogger<MarkStateManager> logger)
        {
            _markStateCrud = markStateCrud;
            _parametersService = parametersService;
            _logger = logger;
            _configuration = _parametersService.Current();
        }

        public async Task<MarkInformation> GetMarkInformation(string sgtin)
        {
            try
            {
                if (!_configuration.Database.OfflineCheckIsEnabled)
                {
                    _logger.LogInformation("Offline проверка отключена");
                    return new MarkInformation();
                }

                var markInfo = await _markStateCrud.GetAsync(sgtin);
                _logger.LogInformation("Получена информация о марке {Sgtin}", sgtin);
                return markInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении информации о марке {Sgtin}", sgtin);
                return new MarkInformation();
            }
        }

        public async Task<Result> SaveMarkInformation(string sgtin, CheckMarksDataTrueApi trueMarkData)
        {
            try
            {
                if (!_configuration.Database.OfflineCheckIsEnabled)
                {
                    _logger.LogInformation("Сохранение в базу данных отключено");
                    return Result.Success();
                }

                var currentMarkState = await _markStateCrud.GetAsync(sgtin);

                foreach (var markCodeData in trueMarkData.Codes)
                {
                    var markState = CreateMarkState(sgtin, markCodeData, currentMarkState, trueMarkData);
                    await _markStateCrud.AddAsync(markState);
                }

                _logger.LogInformation("Информация о марке {Sgtin} сохранена", sgtin);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сохранении информации о марке {Sgtin}", sgtin);
                return Result.Failure(ex.Message);
            }
        }

        public async Task<Result> UpdateMarkState(string sgtin, string newState)
        {
            try
            {
                if (!_configuration.Database.OfflineCheckIsEnabled)
                {
                    return Result.Success();
                }

                var markInfo = await _markStateCrud.GetAsync(sgtin);
                markInfo.State = newState;
                await _markStateCrud.AddAsync(markInfo);

                _logger.LogInformation("Состояние марки {Sgtin} обновлено на {State}", sgtin, newState);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении состояния марки {Sgtin}", sgtin);
                return Result.Failure(ex.Message);
            }
        }

        public async Task<bool> MarkIsSold(string sgtin)
        {
            if (!_configuration.Database.OfflineCheckIsEnabled)
            {
                return false;
            }

            var markInfo = await _markStateCrud.GetAsync(sgtin);
            return markInfo.State == MarkState.Sold;
        }

        private static MarkInformation CreateMarkState(
            string sgtin,
            CodeDataTrueApi markCodeData,
            MarkInformation currentMarkState,
            CheckMarksDataTrueApi trueMarkData)
        {
            string state = string.IsNullOrEmpty(currentMarkState.State)
                ? MarkState.Stock
                : currentMarkState.State;

            return new MarkInformation
            {
                MarkId = sgtin,
                State = markCodeData.Sold ? MarkState.Sold : state,
                TrueApiCisData = markCodeData,
                TrueApiAnswerProperties = new()
                {
                    Code = trueMarkData.Code,
                    Description = trueMarkData.Description,
                    ReqId = trueMarkData.ReqId,
                    ReqTimestamp = trueMarkData.ReqTimestamp
                }
            };
        }
    }
}
