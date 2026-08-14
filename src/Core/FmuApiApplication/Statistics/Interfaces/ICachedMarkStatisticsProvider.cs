using FmuApiApplication.Monitoring.Dto;

namespace FmuApiApplication.Statistics.Interfaces;

public interface ICachedMarkStatisticsProvider
{
    Task<MarkChecksInformation> RestoreCachedStatistic(string cacheKey, int days);
}
