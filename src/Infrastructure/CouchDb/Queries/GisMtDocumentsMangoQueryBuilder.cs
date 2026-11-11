namespace CouchDb.Queries;

/// <summary>
/// Собирает mango-запросы загруженных входящих документов ГИС МТ.
/// </summary>
public static class GisMtDocumentsMangoQueryBuilder
{
    public static Dictionary<string, object> BuildPageQuery(int page, int pageSize) => new()
    {
        ["selector"] = new Dictionary<string, object>
        {
            ["data"] = new Dictionary<string, object> { ["$exists"] = true }
        },
        ["sort"] = new[] { new Dictionary<string, string> { ["data.loadedAt"] = "desc" } },
        ["limit"] = pageSize,
        ["skip"] = (page - 1) * pageSize
    };
}
