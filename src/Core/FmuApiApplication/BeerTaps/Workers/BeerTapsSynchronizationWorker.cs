using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Fmu.BeerTaps.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.BeerTaps.Workers;

public class BeerTapsSynchronizationWorker : BackgroundService
{
    private ILogger<BeerTapsSynchronizationWorker> _logger;
    private readonly IParametersService _parametersService;
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly TimeSpan _startDelay = TimeSpan.FromSeconds(45);

    public BeerTapsSynchronizationWorker(ILogger<BeerTapsSynchronizationWorker> logger, IParametersService parametersService, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _parametersService = parametersService;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(_startDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var _parameters = await _parametersService.CurrentAsync();

            var syncDelay = _parameters.ConnectedFrontolSettings.SyncBeerTapsSettings.SyncBeerTapsPeriodSeconds;
            syncDelay = syncDelay == 0 ? 30 : syncDelay;

            if (_parameters.ConnectedFrontolSettings.SyncBeerTapsSettings.SyncBeerTapsEnabled) 
            {
                _logger.LogInformation("Начинаю синхронизацию пивных кранов...");

                using var scope = _scopeFactory.CreateScope();
                var beerTapsManager = scope.ServiceProvider.GetRequiredService<IBeerOnTapManager>();

                await beerTapsManager.SyncFrontolBeerTaps(_parameters.ConnectedFrontolSettings.ConnectionSettings);
            }

            await Task.Delay(TimeSpan.FromSeconds(syncDelay), stoppingToken);
        }
    }
}
