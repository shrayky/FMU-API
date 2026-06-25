using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Repositories;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CouchDb.Workers;

public class ClearingStorageOfStatisticsWorker : BackgroundService
{
    private readonly ILogger<ClearingStorageOfStatisticsWorker> _logger;
    private readonly IApplicationState _applicationState;
    private readonly IParametersService _parametersService;
    private readonly IServiceScopeFactory _scopeFactory;

    private const int StartDelay = 60;
    private const int WorkIntervalHours = 1;

    public ClearingStorageOfStatisticsWorker(ILogger<ClearingStorageOfStatisticsWorker> logger, IApplicationState applicationState, IParametersService parametersService, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _applicationState = applicationState;
        _parametersService = parametersService;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(StartDelay), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var appConfig = await _parametersService.CurrentAsync();
            var databaseConfig = appConfig.Database;

            var dbOnline = _applicationState.CouchDbOnline();

            if (databaseConfig.Enable && dbOnline && databaseConfig.ClearStorageOfStatistics)
            {
                var dateToCutStorage = DateTime.Now.AddDays(-1 * databaseConfig.DepthOfStorageOfStatisticsInDays);

                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<ICheckStatisticRepository>();

                await repo.ClearStorageToDay(dateToCutStorage, stoppingToken);
            }

            await Task.Delay(TimeSpan.FromHours(WorkIntervalHours), stoppingToken);
        }
    }
}
