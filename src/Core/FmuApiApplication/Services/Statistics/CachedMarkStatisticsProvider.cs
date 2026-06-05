using FmuApiApplication.Services.Statistics.Interfaces;
using FmuApiDomain.Attributes;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.MarkInformation.Models;
using FmuApiDomain.ViewData.ApplicationMonitoring.Dto;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Services.Statistics;

[AutoRegisterService(ServiceLifetime.Scoped)]
public class CachedMarkStatisticsProvider : ICachedMarkStatisticsProvider
{
    public const string Key7days = "check-marks-last-7";
    public const string Key30days = "check-marks-last-30";

    private readonly IMemoryCache _cache;
    private readonly IMarkStatisticsService _markStatisticsService;
    private readonly ILogger<CachedMarkStatisticsProvider> _logger;

    public CachedMarkStatisticsProvider(
        IMemoryCache cache,
        IMarkStatisticsService markStatisticsService,
        ILogger<CachedMarkStatisticsProvider> logger)
    {
        _cache = cache;
        _markStatisticsService = markStatisticsService;
        _logger = logger;
    }

    public async Task<MarkChecksInformation> RestoreCachedStatistic(string cacheKey, int days)
    {
        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);
        var endOfDay = DateTime.UtcNow.Date.AddDays(1);

        var result = await _cache.GetOrCreateAsync(cacheKey, async cacheEntry =>
        {
            cacheEntry.AbsoluteExpiration = endOfDay;

            // Получаем данные за период до вчерашнего дня
            var fromDate = yesterday.AddDays(-days);

            MarkCheckStatistics data;

            try
            {
                data = await _markStatisticsService.ByDays(fromDate, yesterday);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Получение статистики за {Days} дней", days);
                return new MarkChecksInformation();
            }

            return ToMarkChecksInformation(data);
        });

        return result ?? new MarkChecksInformation();
    }

    private static MarkChecksInformation ToMarkChecksInformation(MarkCheckStatistics data) => new()
    {
        Total = data.Total,
        SuccessfulOffline = data.SuccessfulOfflineChecks,
        SuccessfulOnline = data.SuccessfulOnlineChecks,
        SuccessRate = data.SuccessRatePercentage
    };
}
