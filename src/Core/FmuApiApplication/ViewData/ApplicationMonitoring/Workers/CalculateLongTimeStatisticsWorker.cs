using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.MarkInformation.Models;
using FmuApiDomain.ViewData.ApplicationMonitoring.Dto;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.ViewData.ApplicationMonitoring.Workers;

public class CalculateLongTimeStatisticsWorker : BackgroundService
{
    private readonly ILogger<CalculateLongTimeStatisticsWorker> _logger;
    private readonly IMemoryCache _cache;
    private readonly IServiceProvider _serviceProvider;

    private const int CheckPeriodMinutes = 60;
    private const int StartTimeoutPauseMinutes = 5;
    private const string Key7days = "check-marks-last-7";
    private const string Key30days = "check-marks-last-30";
    
    public CalculateLongTimeStatisticsWorker(ILogger<CalculateLongTimeStatisticsWorker> logger, IMemoryCache cache, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _cache = cache;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        
        await Task.Delay(TimeSpan.FromMinutes(StartTimeoutPauseMinutes), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var markStatisticsService = scope.ServiceProvider.GetRequiredService<IMarkStatisticsService>();
            
            await RestoreCachedStatistic(markStatisticsService, Key7days, 7);
            await RestoreCachedStatistic(markStatisticsService, Key30days, 30);
            
            await Task.Delay(TimeSpan.FromMinutes(CheckPeriodMinutes), stoppingToken);
        }
    }
    
    private async Task<MarkChecksInformation> RestoreCachedStatistic(IMarkStatisticsService markStatisticsService, string cacheKey, int days)
    {
        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);
        var endOfDay = DateTime.UtcNow.Date.AddDays(1);
    
        var result = (await _cache.GetOrCreateAsync<MarkChecksInformation>(cacheKey, async cacheEntry =>
        {
            cacheEntry.AbsoluteExpiration = endOfDay;
        
            // Получаем данные за период до вчерашнего дня
            var fromDate = yesterday.AddDays(-days);

            MarkCheckStatistics data;
            
            try
            {
                data = await markStatisticsService.ByDays(fromDate, yesterday);
            }
            catch (Exception e)
            {
                return new MarkChecksInformation();
            }

            return new MarkChecksInformation()
            {
                Total = data.Total,
                SuccessfulOffline = data.SuccessfulOfflineChecks,
                SuccessfulOnline = data.SuccessfulOnlineChecks,
                SuccessRate = data.SuccessRatePercentage
            };
        }));

        return result ?? new MarkChecksInformation();
    }
    
}