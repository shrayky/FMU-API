namespace CouchDb.DatabaseScheme
{
    public static class DatabaseNames
    {
        public const string MarksDbName = "fmu-api-marks";
        public const string DocumentsDbName = "fmu-api-documents";
        public const string MarkCheckingStatistic = "fmu-api-mark-checking-statistic";
        public const string BeerOnTaps = "fmu-api-beer-on-taps";
        public const string GisMtDocumentsDbName = "fmu-api-gis-mt-documents";
        public const string GisMtMarksDbName = "fmu-api-gis-mt-marks";
        public const string GtinCatalogDbName = "fmu-api-gtin-catalog";
        public static string[] Names() =>
        [
            MarksDbName,
            DocumentsDbName,
            MarkCheckingStatistic,
            BeerOnTaps,
            GisMtDocumentsDbName,
            GisMtMarksDbName,
            GtinCatalogDbName
        ];
    }
}
