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

    [Fact]
    public void PageQuery_при_отборе_просроченных_фильтрует_только_по_дате()
    {
        var expiredBefore = new DateTime(2026, 11, 9, 14, 0, 0, DateTimeKind.Utc);

        var query = GisMtMarkMangoQueryBuilder.BuildPageQuery("", "milk", page: 1, pageSize: 50, expiredBeforeUtc: expiredBefore);

        var conditions = Conditions(query);

        var expireCondition = Assert.Single(conditions, condition => condition.ContainsKey("data.expireDate"));
        var expireRange = Assert.IsType<Dictionary<string, object>>(expireCondition["data.expireDate"]);
        Assert.Equal(expiredBefore, expireRange["$lt"]);

        Assert.DoesNotContain(conditions, condition => condition.ContainsKey("data.sold"));
    }

    [Fact]
    public void PageQuery_при_отборе_по_статусу_продажи_ставит_sold_без_фильтра_даты()
    {
        var query = GisMtMarkMangoQueryBuilder.BuildPageQuery("", null, page: 1, pageSize: 50, sold: false);

        var conditions = Conditions(query);

        var soldCondition = Assert.Single(conditions, condition => condition.ContainsKey("data.sold"));
        Assert.False(Assert.IsType<bool>(soldCondition["data.sold"]));

        Assert.DoesNotContain(conditions, condition => condition.ContainsKey("data.expireDate"));
    }

    [Fact]
    public void PageQuery_складывает_поиск_группу_просрочку_и_статус_продажи()
    {
        var expiredBefore = new DateTime(2026, 11, 9, 14, 0, 0, DateTimeKind.Utc);

        var query = GisMtMarkMangoQueryBuilder.BuildPageQuery(
            "01046",
            "milk",
            page: 2,
            pageSize: 50,
            expiredBeforeUtc: expiredBefore,
            sold: false);

        var conditions = Conditions(query);

        Assert.Equal(4, conditions.Count);
        Assert.Contains(conditions, condition => condition.ContainsKey("$or"));
        Assert.Contains(conditions, condition => condition.ContainsKey("data.productGroup"));
        Assert.Contains(conditions, condition => condition.ContainsKey("data.expireDate"));
        Assert.Contains(conditions, condition => condition.ContainsKey("data.sold"));
        Assert.Equal(50, Convert.ToInt32(query["skip"]));
    }

    [Fact]
    public void PageQuery_при_отборе_по_организации_добавляет_ИНН()
    {
        var query = GisMtMarkMangoQueryBuilder.BuildPageQuery(
            "",
            "milk",
            page: 1,
            pageSize: 50,
            organisationInn: "246412218294");

        var conditions = Conditions(query);

        var innCondition = Assert.Single(conditions, condition => condition.ContainsKey("data.organisationInn"));
        Assert.Equal("246412218294", innCondition["data.organisationInn"]);
    }

    [Fact]
    public void PageQuery_без_отбора_по_организации_не_содержит_ИНН()
    {
        var query = GisMtMarkMangoQueryBuilder.BuildPageQuery("", "milk", page: 1, pageSize: 50);

        var conditions = Conditions(query);

        Assert.DoesNotContain(conditions, condition => condition.ContainsKey("data.organisationInn"));
    }

    [Fact]
    public void PageQuery_без_отборов_не_фильтрует_по_продаже_и_дате()
    {
        var query = GisMtMarkMangoQueryBuilder.BuildPageQuery("", "milk", page: 1, pageSize: 50);

        var selector = Assert.IsType<Dictionary<string, object>>(query["selector"]);

        Assert.Equal("milk", selector["data.productGroup"]);
        Assert.DoesNotContain("$and", selector.Keys);
    }

    private static List<Dictionary<string, object>> Conditions(Dictionary<string, object> query)
    {
        var selector = Assert.IsType<Dictionary<string, object>>(query["selector"]);

        if (!selector.TryGetValue("$and", out var and))
            return [selector];

        return Assert
            .IsAssignableFrom<IEnumerable<object>>(and)
            .Cast<Dictionary<string, object>>()
            .ToList();
    }
}
