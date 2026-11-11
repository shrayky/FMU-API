using FmuApiDomain.GisMt.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sqlite;
using Sqlite.Documents;
using System.Text.Json;
using Xunit;

namespace FmuApiApplication.Tests;

public class GisMtMarksSchemaTests
{
    [Fact]
    public void Колонка_ИНН_добавляется_в_существующую_таблицу_и_заполняется_из_JSON()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SqliteDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new SqliteDbContext(options);

        context.Database.ExecuteSqlRaw("""
            CREATE TABLE "GisMtMarks" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_GisMtMarks" PRIMARY KEY,
                "Json" TEXT NOT NULL,
                "SGtin" TEXT NOT NULL,
                "Cis" TEXT NOT NULL,
                "ProductGroup" TEXT NOT NULL,
                "InfoLoadedAt" TEXT NOT NULL,
                "Sold" INTEGER NOT NULL,
                "ExpireDate" TEXT NULL
            )
            """);

        var entity = new GisMtMarkEntity
        {
            Id = "sgtin-1",
            SGtin = "sgtin-1",
            Cis = "cis-1",
            ProductGroup = "milk",
            OrganisationInn = "246412218294",
            SourceDocumentId = "УПД-1",
            InfoLoadedAt = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        context.Database.ExecuteSqlRaw(
            """INSERT INTO "GisMtMarks" ("Id","Json","SGtin","Cis","ProductGroup","InfoLoadedAt","Sold") VALUES ('sgtin-1', {0}, 'sgtin-1', 'cis-1', 'milk', '2026-11-01 00:00:00', 0)""",
            JsonSerializer.Serialize(entity, JsonSerializerOptions.Web));

        GisMtMarksSchema.Upgrade(context);

        var row = context.GisMtMarks.Single();
        Assert.Equal("246412218294", row.OrganisationInn);
        Assert.Equal("УПД-1", row.SourceDocumentId);
    }

    [Fact]
    public void Повторный_апгрейд_ничего_не_ломает()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SqliteDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new SqliteDbContext(options);
        context.Database.EnsureCreated();

        GisMtMarksSchema.Upgrade(context);
        GisMtMarksSchema.Upgrade(context);

        Assert.Empty(context.GisMtMarks);
    }
}
