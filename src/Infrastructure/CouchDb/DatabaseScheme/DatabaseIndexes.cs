using CouchDb.Models;

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
}
