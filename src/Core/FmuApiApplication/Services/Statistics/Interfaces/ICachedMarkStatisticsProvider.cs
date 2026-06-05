using FmuApiDomain.ViewData.ApplicationMonitoring.Dto;

namespace FmuApiApplication.Services.Statistics.Interfaces;

public interface ICachedMarkStatisticsProvider
{
    Task<MarkChecksInformation> RestoreCachedStatistic(string cacheKey, int days);
}
