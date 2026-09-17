using CouchDb.Queries;
using Xunit;

namespace FmuApiApplication.Tests;

public class MarkMangoQueryBuilderTests
{
    [Fact]
    public void ListQuery_идёт_по_timestamp_с_limit_и_skip()
    {
        var query = MarkMangoQueryBuilder.BuildListQuery(page: 2, pageSize: 50);

        Assert.Equal(50, Convert.ToInt32(query["limit"]));
        Assert.Equal(50, Convert.ToInt32(query["skip"]));

        var selector = Assert.IsType<Dictionary<string, object>>(query["selector"]);
        var timestamp = Assert.IsType<Dictionary<string, object>>(selector["data.trueApiAnswerProperties.reqTimestamp"]);
        Assert.Equal(0L, Convert.ToInt64(timestamp["$gte"]));
        Assert.False(selector.ContainsKey("data"));

        var sort = Assert.IsType<Dictionary<string, string>[]>(query["sort"]);
        Assert.Equal("desc", sort[0]["data.trueApiAnswerProperties.reqTimestamp"]);

        var json = System.Text.Json.JsonSerializer.Serialize(query);
        Assert.DoesNotContain("$regex", json);
    }

    [Fact]
    public void PrefixSearchQuery_диапазон_по_markId_без_regex()
    {
        var query = MarkMangoQueryBuilder.BuildPrefixSearchQuery("010460", page: 1, pageSize: 50);

        Assert.Equal(51, Convert.ToInt32(query["limit"]));
        Assert.Equal(0, Convert.ToInt32(query["skip"]));

        var selector = Assert.IsType<Dictionary<string, object>>(query["selector"]);
        var markId = Assert.IsType<Dictionary<string, object>>(selector["data.markId"]);
        Assert.Equal("010460", markId["$gte"]);
        Assert.Equal("010460" + "\ufff0", markId["$lte"]);
        Assert.False(selector.ContainsKey("data"));

        var sort = Assert.IsType<Dictionary<string, string>[]>(query["sort"]);
        Assert.Equal("asc", sort[0]["data.markId"]);

        var json = System.Text.Json.JsonSerializer.Serialize(query);
        Assert.DoesNotContain("$regex", json);
    }

    [Fact]
    public void PrefixSearchQuery_для_второй_страницы_считает_skip()
    {
        var query = MarkMangoQueryBuilder.BuildPrefixSearchQuery("01", page: 3, pageSize: 50);

        Assert.Equal(51, Convert.ToInt32(query["limit"]));
        Assert.Equal(100, Convert.ToInt32(query["skip"]));
    }

    [Fact]
    public void ResolveSearchPagination_полная_страница_с_запасом_открывает_следующую()
    {
        var (count, totalPages) = MarkMangoQueryBuilder.ResolveSearchPagination(page: 1, pageSize: 50, fetchedCount: 51);

        Assert.Equal(51, count);
        Assert.Equal(2, totalPages);
    }

    [Fact]
    public void ResolveSearchPagination_неполная_страница_это_последняя()
    {
        var (count, totalPages) = MarkMangoQueryBuilder.ResolveSearchPagination(page: 2, pageSize: 50, fetchedCount: 10);

        Assert.Equal(60, count);
        Assert.Equal(2, totalPages);
    }
}
