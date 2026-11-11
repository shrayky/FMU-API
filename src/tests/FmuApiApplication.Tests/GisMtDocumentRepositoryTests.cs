using FmuApiApplication.State;
using FmuApiDomain.Configuration;
using FmuApiDomain.GisMt.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sqlite;
using Sqlite.Repositories;
using Xunit;

namespace FmuApiApplication.Tests;

public class GisMtDocumentRepositoryTests
{
    [Fact]
    public async Task List_сортирует_по_дате_загрузки_и_делит_на_страницы()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SqliteDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new SqliteDbContext(options);
        context.Database.EnsureCreated();

        var repository = Repository(context);

        await repository.Save(Document("doc-old", new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc)));
        await repository.Save(Document("doc-new", new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc)));
        await repository.Save(Document("doc-middle", new DateTime(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc)));

        var first = await repository.List(1, 2);

        Assert.True(first.IsSuccess);
        Assert.Equal(3, first.Value.Count);
        Assert.Equal(1, first.Value.CurrentPage);
        Assert.Equal(2, first.Value.PageSize);
        Assert.Equal(2, first.Value.TotalPages);
        Assert.Equal(["doc-new", "doc-middle"], first.Value.Documents.Select(document => document.Number));

        var second = await repository.List(2, 2);

        Assert.True(second.IsSuccess);
        Assert.Equal(2, second.Value.CurrentPage);
        Assert.Equal(["doc-old"], second.Value.Documents.Select(document => document.Number));
    }

    [Fact]
    public async Task List_на_пустой_таблице_возвращает_одну_страницу()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SqliteDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new SqliteDbContext(options);
        context.Database.EnsureCreated();

        var page = await Repository(context).List(1, 50);

        Assert.True(page.IsSuccess);
        Assert.Empty(page.Value.Documents);
        Assert.Equal(0, page.Value.Count);
        Assert.Equal(1, page.Value.TotalPages);
    }

    [Fact]
    public async Task List_не_отдаёт_тело_документа_а_Get_отдаёт()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SqliteDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new SqliteDbContext(options);
        context.Database.EnsureCreated();

        const string body = "{\"number\":\"doc-body\",\"cisesList\":[]}";

        var repository = Repository(context);
        var document = Document("doc-body", new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc));
        document.Body = body;

        await repository.Save(document);

        var page = await repository.List(1, 50);

        Assert.True(page.IsSuccess);
        Assert.Equal(string.Empty, Assert.Single(page.Value.Documents).Body);

        var stored = await repository.Get("doc-body");

        Assert.Equal(body, stored!.Body);
    }

    private static GisMtDocumentRepository Repository(SqliteDbContext context)
    {
        var parameters = new Parameters();
        parameters.Database.Enable = true;

        return new GisMtDocumentRepository(
            NullLogger<GisMtDocumentRepository>.Instance,
            context,
            new FakeParameters(parameters),
            new ApplicationState());
    }

    private static GisMtDocumentEntity Document(string number, DateTime loadedAt) => new()
    {
        Id = number,
        Number = number,
        DocDate = loadedAt.Date,
        Type = "UNIVERSAL_TRANSFER_DOCUMENT",
        Status = "ACCEPTED",
        SenderInn = "2464122182",
        ReceiverInn = "246412218292",
        ProductGroup = "milk",
        OrganisationInn = "246412218292",
        MarksCount = 2,
        LoadedAt = loadedAt
    };
}
