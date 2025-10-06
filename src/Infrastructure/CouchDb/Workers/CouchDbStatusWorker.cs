using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Database.Interface;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CouchDb.Workers
{
    class CouchDbStatusWorker : BackgroundService
    {
        private readonly ILogger<CouchDbStatusWorker> _logger;
        private readonly IParametersService _parametersService;
        private readonly IApplicationState _applicationState;
        private readonly IStatusDbService _statusDbService;

        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10);

        public CouchDbStatusWorker(ILogger<CouchDbStatusWorker> logger, IParametersService parametersService, IApplicationState applicationState, IStatusDbService statusDbService )
        {
            _logger = logger;
            _parametersService = parametersService;
            _applicationState = applicationState;
            _statusDbService = statusDbService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var needToEnsureDatabaseExist = true;

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(_checkInterval, stoppingToken);

                var appConfig = await _parametersService.CurrentAsync();
                var databaseConfig = appConfig.Database;

                await CheckCouchOnlineState(databaseConfig, stoppingToken);

                if (needToEnsureDatabaseExist)
                    needToEnsureDatabaseExist = !await EnsureDatabasesExists(databaseConfig, stoppingToken);

#if DEBUG
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
#endif
            }

        }

        private async Task CheckCouchOnlineState(CouchDbConnection databaseConfig, CancellationToken stoppingToken)
        {
            var dbOnline = _applicationState.CouchDbOnline();

            if (!databaseConfig.Enable && dbOnline)
            {
                _logger.LogCritical("Изменение статуса доступности базы данных, новый статус - отключена");
                _applicationState.UpdateCouchDbState(false);
                return;
            }

            if (!databaseConfig.Enable)
                return;

            var nowState = await _statusDbService.CheckAvailability(databaseConfig.NetAddress, stoppingToken);

            if (nowState == dbOnline)
                return;

            _logger.LogCritical("Изменение статуса доступности базы данных {beforeCheck} -> {aftetCheck}", dbOnline, nowState);
            _applicationState.UpdateCouchDbState(nowState);
        }

        private async Task<bool> EnsureDatabasesExists(CouchDbConnection databaseConfig, CancellationToken stoppingToken)
        {
            var dbOnline = _applicationState.CouchDbOnline();

            if (!dbOnline)
                return false;

            var dbExists = await _statusDbService.EnsureDatabasesExists(databaseConfig, DatabaseNames.Names(), stoppingToken);

            if (!dbExists)
                return false;

            await _statusDbService.EnsureIndexesExist(databaseConfig, stoppingToken);
            
            return true;
        }

    }
}
