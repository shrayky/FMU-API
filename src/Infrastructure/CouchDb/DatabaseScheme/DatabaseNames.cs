using FmuApiDomain.Configuration.Options;

namespace CouchDb.DatabaseScheme
{
    public static class DatabaseNames
    {
        public const string MarksDbName = "fmu-api-marks";
        public const string DocumentsDbName = "fmu-api-documents";
        public const string MarkCheckingStatistic = "fmu-api-mark-checking-statistic";
        public const string BeerOnTaps = "fmu-api-beer-on-taps";
        public static string[] Names() => [MarksDbName, DocumentsDbName, MarkCheckingStatistic, BeerOnTaps];
    }
}
