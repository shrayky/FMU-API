using FmuApiDomain.MarkInformation.Models;

namespace FmuApiDomain.MarkInformation.Interfaces
{
    public interface IMarkStatisticsService
    {
        Task<MarkCheckStatistics> Today();
        Task<MarkCheckStatistics> LastWeek();
        Task<MarkCheckStatistics> LastMonth();
        Task<MarkCheckStatistics> ByDays(DateTime fromDate, DateTime toDate);
    }
}
