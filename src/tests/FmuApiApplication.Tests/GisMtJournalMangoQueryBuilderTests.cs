using CouchDb.Queries;
using FmuApiDomain.GisMt.Journal;
using Xunit;

namespace FmuApiApplication.Tests;

public class GisMtJournalMangoQueryBuilderTests
{
    private static readonly DateTime SentAfter = new(2026, 10, 11, 0, 0, 0);

    [Fact]
    public void Запрос_КИ_фильтрует_организацию_группу_статус_и_дату_отправки()
    {
        var query = GisMtJournalMangoQueryBuilder.BuildSubmittedQuery("2464122182", "water", SentAfter, 500);
        var selector = Assert.IsType<Dictionary<string, object>>(query["selector"]);

        Assert.Equal("2464122182", selector["data.inn"]);
        Assert.Equal("water", selector["data.productGroup"]);
        Assert.Equal(GisMtJournalStatuses.Sent, selector["data.status"]);
        Assert.True(selector.ContainsKey("data.sentAt"));
        Assert.False(selector.ContainsKey("data.createdAt"));

        var sort = Assert.IsType<Dictionary<string, string>[]>(query["sort"]);
        Assert.Equal("desc", Assert.Single(sort)["data.sentAt"]);
        Assert.Equal(GisMtJournalMangoQueryBuilder.SubmittedIndex, query["use_index"]);
    }

    [Fact]
    public void Старые_акты_без_даты_отправки_ищутся_по_дате_создания()
    {
        var query = GisMtJournalMangoQueryBuilder.BuildSubmittedWithoutSentAtQuery("2464122182", "water", SentAfter, 500);
        var selector = Assert.IsType<Dictionary<string, object>>(query["selector"]);

        Assert.True(selector.ContainsKey("$or"));
        Assert.True(selector.ContainsKey("data.createdAt"));
        Assert.False(selector.ContainsKey("data.sentAt"));

        var sort = Assert.IsType<Dictionary<string, string>[]>(query["sort"]);
        Assert.Equal("desc", Assert.Single(sort)["data.createdAt"]);
        Assert.Equal(GisMtJournalMangoQueryBuilder.CreatedAtIndex, query["use_index"]);
    }
}
