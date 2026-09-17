namespace CouchDb.Queries;

/// <summary>
/// Собирает mango-запросы справочника проверенных марок так, чтобы CouchDB шёл по btree-индексам.
/// </summary>
public static class MarkMangoQueryBuilder
{
    public const string TimestampField = "data.trueApiAnswerProperties.reqTimestamp";
    public const string MarkIdField = "data.markId";
    private const string PrefixUpperBoundSuffix = "\ufff0";

    /// <summary>
    /// Страница справочника: селектор по timestamp, чтобы CouchDB взял timeStamp-data-idx.
    /// </summary>
    public static Dictionary<string, object> BuildListQuery(int page, int pageSize)
    {
        return new Dictionary<string, object>
        {
            ["selector"] = new Dictionary<string, object>
            {
                [TimestampField] = new Dictionary<string, object> { ["$gte"] = 0L }
            },
            ["sort"] = new[] { new Dictionary<string, string> { [TimestampField] = "desc" } },
            ["limit"] = pageSize,
            ["skip"] = Skip(page, pageSize)
        };
    }

    /// <summary>
    /// Поиск по началу номера марки через диапазон mark-id-idx вместо $regex.
    /// </summary>
    public static Dictionary<string, object> BuildPrefixSearchQuery(string searchTerm, int page, int pageSize)
    {
        return new Dictionary<string, object>
        {
            ["selector"] = new Dictionary<string, object>
            {
                [MarkIdField] = new Dictionary<string, object>
                {
                    ["$gte"] = searchTerm,
                    ["$lte"] = searchTerm + PrefixUpperBoundSuffix
                }
            },
            ["sort"] = new[] { new Dictionary<string, string> { [MarkIdField] = "asc" } },
            ["limit"] = pageSize + 1,
            ["skip"] = Skip(page, pageSize)
        };
    }

    /// <summary>
    /// Считает count и число страниц по выборке pageSize+1, без полного скана совпадений.
    /// </summary>
    public static (int Count, int TotalPages) ResolveSearchPagination(int page, int pageSize, int fetchedCount)
    {
        var skip = Skip(page, pageSize);
        var hasMore = fetchedCount > pageSize;
        var pageCount = Math.Min(fetchedCount, pageSize);
        var count = skip + pageCount + (hasMore ? 1 : 0);
        var totalPages = Math.Max(1, (int)Math.Ceiling(count / (double)pageSize));

        return (count, totalPages);
    }

    private static int Skip(int page, int pageSize) => (page - 1) * pageSize;
}
