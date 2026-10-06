using System.Text.Json;
using CouchDb.Queries;
using Xunit;

namespace FmuApiApplication.Tests;

public class MarkCheckStatisticMangoQueryBuilderTests
{
    [Fact]
    public void LastCheckQuery_берёт_несколько_новых_документов_по_индексу()
    {
        var query = MarkCheckStatisticMangoQueryBuilder.BuildLastCheckQuery("046071515600305jtg(a");

        Assert.Equal(MarkCheckStatisticMangoQueryBuilder.LastCheckCandidateLimit, Convert.ToInt32(query["limit"]));
        Assert.Equal(MarkCheckStatisticMangoQueryBuilder.SgtinCheckDateIndex, query["use_index"]);
        Assert.False(query.ContainsKey("skip"));

        var selector = Assert.IsType<Dictionary<string, object>>(query["selector"]);
        Assert.Equal("046071515600305jtg(a", selector["data.sGtin"]);

        var checkDate = Assert.IsType<Dictionary<string, object>>(selector["data.checkDate"]);
        Assert.Equal("0001-01-01T00:00:00", checkDate["$gte"]);

        var sort = Assert.IsType<Dictionary<string, string>[]>(query["sort"]);
        Assert.Equal("asc", sort[0]["data.sGtin"]);
        Assert.Equal("desc", sort[1]["data.checkDate"]);

        var json = JsonSerializer.Serialize(query);
        Assert.DoesNotContain("$in", json);
    }
}
