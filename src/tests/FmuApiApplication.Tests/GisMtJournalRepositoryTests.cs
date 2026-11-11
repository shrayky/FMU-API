using FmuApiApplication.State;
using FmuApiDomain.Configuration;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Journal;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sqlite;
using Sqlite.Repositories;
using Xunit;

namespace FmuApiApplication.Tests;

public class GisMtJournalRepositoryTests
{
    private static readonly DateTime Now = new(2026, 11, 10, 12, 0, 0);

    [Fact]
    public async Task Статус_меняется_только_у_акта_в_ожидаемом_состоянии()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        using var context = Context(connection);
        var repository = Repository(context);
        var document = Document("doc-1", GisMtJournalStatuses.Draft, Now);

        await repository.Save(document);

        document.Status = GisMtJournalStatuses.Sending;

        Assert.True(await repository.TryUpdateStatus(document, GisMtJournalStatuses.Draft));
        Assert.False(await repository.TryUpdateStatus(document, GisMtJournalStatuses.Draft));

        var stored = await repository.Get("doc-1");
        Assert.Equal(GisMtJournalStatuses.Sending, stored!.Status);
    }

    [Fact]
    public async Task Выборка_КИ_фильтрует_организацию_группу_и_отклонённые_акты()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        using var context = Context(connection);
        var repository = Repository(context);

        await repository.Save(SentDocument("sent-ok", "2464122182", "water", []));
        await repository.Save(SentDocument("sent-rejected", "2464122182", "water", [], GisMtCrptStatuses.CheckedNotOk));
        await repository.Save(SentDocument("sent-other-inn", "7700000000", "water", []));
        await repository.Save(SentDocument("sent-other-group", "2464122182", "milk", []));
        await repository.Save(SentDocument("sent-old", "2464122182", "water", [], sentAt: Now.AddDays(-40)));

        var cises = await repository.GetSubmittedCis("2464122182", "water", Now.AddDays(-30), 500);

        Assert.True(cises.IsSuccess);
        Assert.Equal(["cis-sent-ok"], cises.Value);
    }

    private static SqliteDbContext Context(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<SqliteDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new SqliteDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }

    private static GisMtJournalRepository Repository(SqliteDbContext context)
    {
        var parameters = new Parameters();
        parameters.Database.Enable = true;

        return new GisMtJournalRepository(
            NullLogger<GisMtJournalRepository>.Instance,
            context,
            new FakeParameters(parameters),
            new ApplicationState());
    }

    private static GisMtJournalEntity Document(string id, string status, DateTime sentAt) => new()
    {
        Id = id,
        Kind = GisMtJournalKinds.LkReceipt,
        Status = status,
        Inn = "2464122182",
        ProductGroup = "water",
        CreatedAt = sentAt.AddMinutes(-1),
        SentAt = status == GisMtJournalStatuses.Sent ? sentAt : null,
        Lines = [new GisMtJournalLine { Cis = $"cis-{id}" }]
    };

    private static GisMtJournalEntity SentDocument(
        string id,
        string inn,
        string productGroup,
        List<GisMtJournalLine>? lines,
        string crptStatus = "",
        DateTime? sentAt = null)
    {
        var document = Document(id, GisMtJournalStatuses.Sent, sentAt ?? Now);
        document.Inn = inn;
        document.ProductGroup = productGroup;
        document.CrptStatus = crptStatus;
        document.Lines = lines is { Count: > 0 } ? lines : [new GisMtJournalLine { Cis = $"cis-{id}" }];

        return document;
    }
}
