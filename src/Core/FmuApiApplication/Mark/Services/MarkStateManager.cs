// Ignore Spelling: Fmu

using CSharpFunctionalExtensions;
using FmuApiApplication.Mark.Interfaces;
using FmuApiDomain.Configuration;
using Microsoft.Extensions.Logging;
using FmuApiDomain.TrueApi.MarkData.Check;
using FmuApiDomain.TrueApi.MarkData;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.MarkInformation.Entities;
using FmuApiDomain.MarkInformation.Enums;
using FmuApiDomain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using FmuApiDomain.State.Interfaces;

namespace FmuApiApplication.Mark.Services
{
    public class MarkStateManager : IMarkStateManager
    {
        private readonly IMarkInformationRepository _markStateCrud;
        private readonly IParametersService _parametersService;
        private readonly ILogger<MarkStateManager> _logger;
        private readonly Parameters _configuration;
        private readonly IApplicationState _appState;

        public MarkStateManager(IServiceProvider services)
        {
            _markStateCrud = services.GetRequiredService<IMarkInformationRepository>();
            _parametersService = services.GetRequiredService<IParametersService>();
            _logger = services.GetRequiredService<ILogger<MarkStateManager>>();
            _appState = services.GetRequiredService<IApplicationState>();
            _configuration = _parametersService.Current();
        }

        public async Task<MarkEntity> GetMarkInformation(string sgtin)
        {
            if (!_configuration.Database.DatabaseCheckIsEnabled)
            {
                _logger.LogInformation("Database проверка отключена, проверка марки {sgtin} пропускается.", sgtin);
                return new MarkEntity();
            }

            if (!_appState.CouchDbOnline())
            {
                _logger.LogWarning("Database не в сети, проверка марки {sgtin} пропускается.", sgtin);
                return new MarkEntity();
            }

            try
            {
                var markInfo = await _markStateCrud.GetAsync(sgtin);

                _logger.LogInformation("Получена информация о марке {Sgtin}", sgtin);
                
                return markInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении информации о марке {Sgtin}", sgtin);
            }

            return new MarkEntity();

        }

        public async Task<Result> SaveMarkInformation(string sgtin, CheckMarksDataTrueApi trueMarkData)
        {
            if (!_configuration.Database.DatabaseCheckIsEnabled)
            {
                _logger.LogInformation("Сохранение в базу данных {sgtin} пропущенно, использование базы данных отключено.", sgtin);
                return Result.Success();
            }

            if (!_appState.CouchDbOnline())
            {
                _logger.LogWarning("Сохранение в базу данных {sgtin} пропущенно, базы данных не в сети.", sgtin);
                return Result.Success();
            }

            try
            {
                var currentMarkState = await _markStateCrud.GetAsync(sgtin);

                foreach (var markCodeData in trueMarkData.Codes)
                {
                    var mark = CreateMarkEntity(sgtin, markCodeData, currentMarkState, trueMarkData);
                    await _markStateCrud.AddAsync(mark);
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
            if (!_configuration.Database.DatabaseCheckIsEnabled)
            {
                return Result.Success();
            }

            if (!_appState.CouchDbOnline())
            {
                _logger.LogWarning("Сохранение в базу данных {sgtin} пропущенно, базы данных не в сети.", sgtin);
                return Result.Success();
            }

            try
            {
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
            if (!_configuration.Database.DatabaseCheckIsEnabled)
            {
                return false;
            }

            if (!_appState.CouchDbOnline())
            {
                return false;
            }

            var markInfo = await _markStateCrud.GetAsync(sgtin);
            return markInfo.State == MarkState.Sold;
        }

        private static MarkEntity CreateMarkEntity(
            string sgtin,
            CodeDataTrueApi markCodeData,
            MarkEntity currentMarkState,
            CheckMarksDataTrueApi trueMarkData)
        {
            string state = string.IsNullOrEmpty(currentMarkState.State)
                ? MarkState.Stock
                : currentMarkState.State;

            return new MarkEntity
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
