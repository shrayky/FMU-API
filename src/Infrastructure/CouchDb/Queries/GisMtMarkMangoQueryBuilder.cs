namespace CouchDb.Queries;

/// <summary>
/// Собирает mango-запросы поиска остатков марок ГИС МТ.
/// </summary>
public static class GisMtMarkMangoQueryBuilder
{
    private const int DefaultQueryLimit = 1_000_000;

    public static Dictionary<string, object> BuildSelector(string searchTerm, string? productGroup)
    {
        var conditions = new List<object>();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            conditions.Add(new Dictionary<string, object>
            {
                ["$or"] = new object[]
                {
                    new Dictionary<string, object>
                    {
                        ["data.sGtin"] = new Dictionary<string, object> { ["$regex"] = searchTerm }
                    },
                    new Dictionary<string, object>
                    {
                        ["data.cis"] = new Dictionary<string, object> { ["$regex"] = searchTerm }
                    }
                }
            });
        }

        if (!string.IsNullOrWhiteSpace(productGroup))
        {
            conditions.Add(new Dictionary<string, object>
            {
                ["data.productGroup"] = productGroup
            });
        }

        if (conditions.Count == 0)
        {
            return new Dictionary<string, object>
            {
                ["data"] = new Dictionary<string, object> { ["$exists"] = true }
            };
        }

        if (conditions.Count == 1)
            return (Dictionary<string, object>)conditions[0];

        return new Dictionary<string, object>
        {
            ["$and"] = conditions.ToArray()
        };
    }

    public static Dictionary<string, object> BuildPageQuery(
        string searchTerm,
        string? productGroup,
        int page,
        int pageSize)
    {
        return new Dictionary<string, object>
        {
            ["selector"] = BuildSelector(searchTerm, productGroup),
            ["sort"] = new[] { new Dictionary<string, string> { ["data.infoLoadedAt"] = "desc" } },
            ["limit"] = pageSize,
            ["skip"] = (page - 1) * pageSize
        };
    }

    public static Dictionary<string, object> BuildCountQuery(
        string searchTerm,
        string? productGroup,
        int queryLimit)
    {
        return new Dictionary<string, object>
        {
            ["selector"] = BuildSelector(searchTerm, productGroup),
            ["fields"] = new[] { "_id" },
            ["limit"] = queryLimit
        };
    }

    public static int ResolveQueryLimit(int configuredLimit)
    {
        return configuredLimit == 0 ? DefaultQueryLimit : configuredLimit;
    }
}
