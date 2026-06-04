using FmuApiDomain.Configuration.Options;

namespace CouchDb
{
    public static class DatabaseNames
    {
        public const string MarksDbName = "fmu-api-marks";
        public const string DocumentsDbName = "fmu-api-documents";
        public const string MarkCheckingStatistic = "fmu-api-mark-checking-statistic";
        public const string BeerOnTaps = "fmu-api-beer-on-taps";
        public static string[] Names() => [MarksDbName, DocumentsDbName, MarkCheckingStatistic, BeerOnTaps];

        // устаревшие:
        [Obsolete]
        public static string MarksStateDb { get; private set; } = string.Empty;
        [Obsolete]
        public static string FrontolDocumentsDb { get; private set; } = string.Empty;
        [Obsolete]
        public static string AlcoStampsDb { get; private set; } = string.Empty;

        [Obsolete]
        public static void Initialize(CouchDbConnection settings)
        {
            if (!string.IsNullOrEmpty(settings.MarksStateDbName))
                MarksStateDb = settings.MarksStateDbName.ToLower();

            if (!string.IsNullOrEmpty(settings.FrontolDocumentsDbName))
                FrontolDocumentsDb = settings.FrontolDocumentsDbName.ToLower();
                
            if (!string.IsNullOrEmpty(settings.AlcoStampsDbName))
                AlcoStampsDb = settings.AlcoStampsDbName.ToLower();
        }
    }
}
