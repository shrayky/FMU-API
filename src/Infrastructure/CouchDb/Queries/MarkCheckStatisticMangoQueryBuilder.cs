namespace CouchDb.Queries;

/// <summary>
/// Запрос последней проверки марки по составному индексу sgtin и даты.
/// CouchDB отдаёт несколько самых новых документов, а не всю историю марки.
/// </summary>
public static class MarkCheckStatisticMangoQueryBuilder
{
    public const string SgtinCheckDateIndex = "sgtin-check-date-idx";
    public const string SgtinField = "data.sGtin";
    public const string CheckDateField = "data.checkDate";
    public const int LastCheckCandidateLimit = 5;

    private const string EarliestCheckDate = "0001-01-01T00:00:00";

    public static Dictionary<string, object> BuildLastCheckQuery(string sgtin)
    {
        return new Dictionary<string, object>
        {
            ["selector"] = new Dictionary<string, object>
            {
                [SgtinField] = sgtin,
                [CheckDateField] = new Dictionary<string, object>
                {
                    ["$gte"] = EarliestCheckDate
                }
            },
            ["sort"] = new[]
            {
                new Dictionary<string, string> { [SgtinField] = "asc" },
                new Dictionary<string, string> { [CheckDateField] = "desc" }
            },
            ["limit"] = LastCheckCandidateLimit,
            ["use_index"] = SgtinCheckDateIndex
        };
    }
}
