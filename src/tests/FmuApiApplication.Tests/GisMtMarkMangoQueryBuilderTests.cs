using CouchDb.Queries;
using Xunit;

namespace FmuApiApplication.Tests;

public class GisMtMarkMangoQueryBuilderTests
{
    [Fact]
    public void PageQuery_при_фильтре_группы_передаёт_limit_skip_и_сортировку()
    {
        var query = GisMtMarkMangoQueryBuilder.BuildPageQuery("", "milk", page: 1, pageSize: 50);

        Assert.Equal(50, Convert.ToInt32(query["limit"]));
        Assert.Equal(0, Convert.ToInt32(query["skip"]));
        Assert.Contains("sort", query.Keys);

        var selector = Assert.IsType<Dictionary<string, object>>(query["selector"]);
        Assert.Equal("milk", selector["data.productGroup"]);
    }

    [Fact]
    public void PageQuery_для_второй_страницы_считает_skip()
    {
        var query = GisMtMarkMangoQueryBuilder.BuildPageQuery("", "milk", page: 2, pageSize: 50);

        Assert.Equal(50, Convert.ToInt32(query["limit"]));
        Assert.Equal(50, Convert.ToInt32(query["skip"]));
    }

    [Fact]
    public void CountQuery_при_фильтре_группы_берёт_только_id_и_QueryLimit()
    {
        var query = GisMtMarkMangoQueryBuilder.BuildCountQuery("", "milk", queryLimit: 1_000_000);

        Assert.Equal(1_000_000, Convert.ToInt32(query["limit"]));
        Assert.DoesNotContain("skip", query.Keys);
        Assert.DoesNotContain("sort", query.Keys);

        var fields = Assert.IsAssignableFrom<IEnumerable<string>>(query["fields"]);
        Assert.Equal(["_id"], fields.ToArray());

        var selector = Assert.IsType<Dictionary<string, object>>(query["selector"]);
        Assert.Equal("milk", selector["data.productGroup"]);
    }

    [Fact]
    public void ResolveQueryLimit_ноль_заменяет_на_миллион()
    {
        Assert.Equal(1_000_000, GisMtMarkMangoQueryBuilder.ResolveQueryLimit(0));
        Assert.Equal(5000, GisMtMarkMangoQueryBuilder.ResolveQueryLimit(5000));
    }
}
