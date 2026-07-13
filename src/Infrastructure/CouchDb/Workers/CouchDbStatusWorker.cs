using CouchDb.DatabaseScheme;
using CouchDb.Interfaces;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Database.Interface;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CouchDb.Workers;

class CouchDbStatusWorker(
    ILogger<CouchDbStatusWorker> logger,
    IParametersService parametersService,
    IApplicationState applicationState,
    IStatusDbService statusDbService,
    IIndexingService indexingService) : BackgroundService
{
    private readonly ILogger<CouchDbStatusWorker> _logger = logger;
    private readonly IParametersService _parametersService = parametersService;
    private readonly IApplicationState _applicationState = applicationState;
    private readonly IStatusDbService _statusDbService = statusDbService;
    private readonly IIndexingService _indexingService = indexingService;

    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var needToEnsureDatabaseExist = true;
        var needToEnsureDatabaseIndex = true;

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_checkInterval, stoppingToken);

            var appConfig = await _parametersService.CurrentAsync();
            var databaseConfig = appConfig.Database;

            await CheckCouchOnlineState(databaseConfig, stoppingToken);

            if (_applicationState.CouchDbOnline())
            {
                if (needToEnsureDatabaseExist)
                    needToEnsureDatabaseExist = !await EnsureDatabasesExists(databaseConfig, stoppingToken);

                if (needToEnsureDatabaseIndex)
                    needToEnsureDatabaseIndex = !await EnsureDatabaseIndexes(databaseConfig, stoppingToken);
            }
                
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
        var dbExists = await _statusDbService.EnsureDatabasesExists(databaseConfig, DatabaseNames.Names(), stoppingToken);

        if (!dbExists)
            return false;

        return true;
    }

    private async Task<bool> EnsureDatabaseIndexes(CouchDbConnection databaseConfig, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Проверка наличия индексов для баз данных CouchDB.");

        var indexEnsureResult = await _indexingService.EnsureIndexesExist(databaseConfig, stoppingToken);

        if (indexEnsureResult.IsSuccess)
        {
            _logger.LogInformation("Индексы для баз данных CouchDB созданы успешно");
        }
        else
        {
            _logger.LogError("Ошибка проверки индексов базы данных CouchDb: {err}", indexEnsureResult.Error);

            return false;
        }

        return true;
    }
}