using FmuApiApplication.Services.Statistics;
using FmuApiApplication.Services.Statistics.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FmuApiApplication.ViewData.ApplicationMonitoring.Workers;

public class CalculateLongTimeStatisticsWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    private const int CheckPeriodMinutes = 60;
    private const int StartTimeoutPauseMinutes = 5;

    public CalculateLongTimeStatisticsWorker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(StartTimeoutPauseMinutes), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var cachedMarkStatisticsProvider = scope.ServiceProvider
                .GetRequiredService<ICachedMarkStatisticsProvider>();

            await cachedMarkStatisticsProvider.RestoreCachedStatistic(
                CachedMarkStatisticsProvider.Key7days, 7);

            await cachedMarkStatisticsProvider.RestoreCachedStatistic(
                CachedMarkStatisticsProvider.Key30days, 30);

            await Task.Delay(TimeSpan.FromMinutes(CheckPeriodMinutes), stoppingToken);
        }
    }
}
