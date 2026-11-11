using FmuApiDomain.GisMt.Journal;

namespace CouchDb.Queries;

/// <summary>
/// Собирает mango-запросы журнала исходящих документов ГИС МТ.
/// </summary>
public static class GisMtJournalMangoQueryBuilder
{
    public const string CreatedAtIndex = "gis-mt-journal-created-at-idx";
    public const string SubmittedIndex = "gis-mt-journal-submitted-idx";

    public static Dictionary<string, object> BuildUidQuery(string uid) => new()
    {
        ["selector"] = new Dictionary<string, object> { ["data.uid"] = uid },
        ["limit"] = 1
    };

    public static Dictionary<string, object> BuildSelector(string kind, string status)
    {
        var conditions = new List<object>();

        if (!string.IsNullOrWhiteSpace(kind))
            conditions.Add(new Dictionary<string, object> { ["data.kind"] = kind });

        if (!string.IsNullOrWhiteSpace(status))
            conditions.Add(new Dictionary<string, object> { ["data.status"] = status });

        if (conditions.Count == 0)
            return new Dictionary<string, object> { ["data"] = new Dictionary<string, object> { ["$exists"] = true } };

        if (conditions.Count == 1)
            return (Dictionary<string, object>)conditions[0];

        return new Dictionary<string, object> { ["$and"] = conditions.ToArray() };
    }

    public static Dictionary<string, object> BuildPageQuery(string kind, string status, int page, int pageSize) => new()
    {
        ["selector"] = BuildSelector(kind, status),
        ["sort"] = new[] { new Dictionary<string, string> { ["data.createdAt"] = "desc" } },
        ["limit"] = pageSize,
        ["skip"] = (page - 1) * pageSize
    };

    public static Dictionary<string, object> BuildCountQuery(string kind, string status, int queryLimit) => new()
    {
        ["selector"] = BuildSelector(kind, status),
        ["fields"] = new[] { "_id" },
        ["limit"] = queryLimit
    };

    /// <summary>
    /// Запрос актов одного статуса для фоновой проверки статуса в ГИС МТ.
    /// </summary>
    public static Dictionary<string, object> BuildStatusQuery(string status, int limit) => new()
    {
        ["selector"] = new Dictionary<string, object> { ["data.status"] = status },
        ["sort"] = new[] { new Dictionary<string, string> { ["data.createdAt"] = "desc" } },
        ["limit"] = limit
    };

    /// <summary>
    /// Запрос КИ отправленных актов организации: без отклонённых ГИС МТ и только за нужный период.
    /// </summary>
    public static Dictionary<string, object> BuildSubmittedQuery(
        string inn,
        string productGroup,
        DateTime sentAfter,
        int limit) => new()
    {
        ["selector"] = SubmittedSelector(inn, productGroup, "data.sentAt", sentAfter),
        ["sort"] = new[] { new Dictionary<string, string> { ["data.sentAt"] = "desc" } },
        ["use_index"] = SubmittedIndex,
        ["limit"] = limit
    };

    /// <summary>
    /// Отправленные акты без даты отправки: у старых записей есть только дата создания.
    /// </summary>
    public static Dictionary<string, object> BuildSubmittedWithoutSentAtQuery(
        string inn,
        string productGroup,
        DateTime createdAfter,
        int limit) => new()
    {
        ["selector"] = new Dictionary<string, object>
        {
            ["data.status"] = GisMtJournalStatuses.Sent,
            ["data.inn"] = inn,
            ["data.productGroup"] = productGroup,
            ["data.crptStatus"] = new Dictionary<string, object>
            {
                ["$nin"] = GisMtCrptStatuses.Failed
            },
            ["$or"] = new object[]
            {
                new Dictionary<string, object>
                {
                    ["data.sentAt"] = null!
                },
                new Dictionary<string, object>
                {
                    ["data.sentAt"] = new Dictionary<string, object> { ["$exists"] = false }
                }
            },
            ["data.createdAt"] = new Dictionary<string, object>
            {
                ["$gte"] = createdAfter
            }
        },
        ["sort"] = new[] { new Dictionary<string, string> { ["data.createdAt"] = "desc" } },
        ["use_index"] = CreatedAtIndex,
        ["limit"] = limit
    };

    private static Dictionary<string, object> SubmittedSelector(
        string inn,
        string productGroup,
        string dateField,
        DateTime after)
    {
        var selector = new Dictionary<string, object>
        {
            ["data.status"] = GisMtJournalStatuses.Sent,
            ["data.inn"] = inn,
            ["data.productGroup"] = productGroup,
            ["data.crptStatus"] = new Dictionary<string, object>
            {
                ["$nin"] = GisMtCrptStatuses.Failed
            }
        };

        selector[dateField] = new Dictionary<string, object>
        {
            ["$gte"] = after
        };

        return selector;
    }
}
