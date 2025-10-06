namespace FmuApiDomain.MarkInformation.Models
{
    public class MarkCheckStatistics
    {
        public int Total { get; init; }
        public int SuccessfulOnlineChecks { get; init; }
        public int SuccessfulOfflineChecks { get; init; }

        public double SuccessRatePercentage => Total > 0 
            ? Math.Round((double)(SuccessfulOnlineChecks + SuccessfulOfflineChecks) / Total * 100, 2) 
            : 0;
    }
}