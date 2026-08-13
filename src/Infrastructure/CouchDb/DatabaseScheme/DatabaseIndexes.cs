namespace CouchDb.DatabaseScheme;

public class DatabaseIndexes
{
    public static Dictionary<string, CouchDbIndexDefinition[]> DatabaseIndexSchema()
    {
        return new Dictionary<string, CouchDbIndexDefinition[]>
            {
                { DatabaseNames.MarksDbName, MarksDbIndexes() },
                { DatabaseNames.MarkCheckingStatistic, MarkCheckStatisticsDbIndexes() },
                { DatabaseNames.BeerOnTaps, BeerOnTapsDbIndexes() },
                { DatabaseNames.GisMtDocumentsDbName, GisMtDocumentsDbIndexes() },
                { DatabaseNames.GisMtMarksDbName, GisMtMarksDbIndexes() },
            };
    }

    private static CouchDbIndexDefinition[] MarksDbIndexes() =>
        [
            new("mark-id-idx", new(["data.markId"])),
            new("mark-data-idx", new(["data"])),
            new("timeStamp-data-idx", new(["data.trueApiAnswerProperties.reqTimestamp"])),
        ];

    private static CouchDbIndexDefinition[] MarkCheckStatisticsDbIndexes() =>
        [
            new ("date-time-idx", new (["data.checkDate"])),
            new ("date-sgtin", new (["data.sGtin"])),
            new ("check-day-idx", new (["data.checkDay"])),
        ];

    private static CouchDbIndexDefinition[] BeerOnTapsDbIndexes() =>
        [
            new("markingCode-idx", new(["data.markingCode"])),
            new("markId-idx", new(["data.markId"])),
        ];

    private static CouchDbIndexDefinition[] GisMtDocumentsDbIndexes() =>
        [
            new("gis-mt-doc-number-idx", new(["data.number"])),
            new("gis-mt-doc-loaded-at-idx", new(["data.loadedAt"])),
        ];

    private static CouchDbIndexDefinition[] GisMtMarksDbIndexes() =>
        [
            new("gis-mt-mark-cis-idx", new(["data.cis"])),
            new("gis-mt-mark-sgtin-idx", new(["data.sGtin"])),
            new("gis-mt-mark-product-group-idx", new(["data.productGroup"])),
            new("gis-mt-mark-product-group-loaded-at-idx", new(["data.productGroup", "data.infoLoadedAt"])),
            new("gis-mt-mark-info-loaded-at-idx", new(["data.infoLoadedAt"])),
            new("gis-mt-mark-cleanup-idx", new(["data.infoLoadedAt", "data.sold"])),
        ];
}
