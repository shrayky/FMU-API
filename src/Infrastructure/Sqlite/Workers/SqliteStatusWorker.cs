using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.State.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sqlite.Workers;

/// <summary>
/// Проверяет доступность файла SQLite и пишет состояние в мониторинг.
/// </summary>
class SqliteStatusWorker(
    ILogger<SqliteStatusWorker> logger,
    IParametersService parametersService,
    IApplicationState applicationState) : BackgroundService
{
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var databaseConfig = (await parametersService.CurrentAsync()).Database;

            CheckState(databaseConfig);

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private void CheckState(CouchDbConnection databaseConfig)
    {
        var nowState = CheckAvailability(databaseConfig);
        var dbOnline = applicationState.CouchDbOnline();

        if (nowState == dbOnline)
            return;

        logger.LogCritical("Изменение статуса доступности базы данных {beforeCheck} -> {afterCheck}", dbOnline, nowState);
        applicationState.UpdateCouchDbState(nowState);
    }

    private bool CheckAvailability(CouchDbConnection databaseConfig)
    {
        if (!databaseConfig.Enable)
            return false;

        try
        {
            using var connection = new SqliteConnection(SqliteConnectionBuilder.Build(databaseConfig));
            connection.Open();

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось открыть файл базы данных SQLite {Path}", databaseConfig.ResolvedSqlitePath);

            return false;
        }
    }
}
