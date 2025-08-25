using FmuApiDomain.MarkInformation.Models;

namespace FmuApiDomain.Repositories
{
    public interface ICheckStatisticRepository
    {
        Task SuccessOnLineCheck(string mark, DateTime checkDate);
        Task OnLineCheckWithWarnings(string mark, DateTime checkDate, string warningMessage);
        Task SuccessOffLineCheck(string mark, DateTime checkDate);
        Task OffLineCheckWithWarnings(string mark, DateTime checkDate, string warningMessage);
        Task FailureCheck(string mark, DateTime checkDate);
        Task<MarkCheckStatistics> CheckStatisticsByDays(DateTime fromDate, DateTime toDate);
    }
}
