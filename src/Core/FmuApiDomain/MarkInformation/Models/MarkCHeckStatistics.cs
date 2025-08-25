namespace FmuApiDomain.MarkInformation.Models
{
    public class MarkCheckStatistics
    {
        public int Total { get; set; }
        public int SuccessfulOnlineChecks { get; set; }
        public int SuccessfulOfflineChecks { get; set; }

        public double SuccessRatePercentage => Total > 0 
            ? Math.Round((double)(SuccessfulOnlineChecks + SuccessfulOfflineChecks) / Total * 100, 2) 
            : 0;
    }
}