using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Journal;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sqlite;
using Sqlite.Documents;
using System.Text.Json;
using Xunit;

namespace FmuApiApplication.Tests;

public class GisMtJournalStorageTests
{
    [Fact]
    public void Таблица_журнала_повторяет_колонки_строки()
    {
        var sql = GisMtJournalSchema.TableSql;

        Assert.Contains("\"GisMtJournal\"", sql);
        Assert.Contains("\"Id\"", sql);
        Assert.Contains("\"Json\"", sql);
        Assert.Contains("\"Uid\"", sql);
        Assert.Contains("\"Kind\"", sql);
        Assert.Contains("\"Status\"", sql);
        Assert.Contains("\"CreatedAt\"", sql);
        Assert.Contains("\"Inn\"", sql);
        Assert.Contains("\"ProductGroup\"", sql);
        Assert.Contains("\"SentAt\"", sql);
        Assert.Contains("\"CrptStatus\"", sql);
    }

    [Fact]
    public void Колонки_выборки_КИ_добавляются_в_существующую_таблицу_и_заполняются()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SqliteDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new SqliteDbContext(options);

        context.Database.ExecuteSqlRaw("""
            CREATE TABLE "GisMtJournal" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_GisMtJournal" PRIMARY KEY,
                "Json" TEXT NOT NULL,
                "Uid" TEXT NULL,
                "Kind" TEXT NOT NULL,
                "Status" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL
            )
            """);

        var sentAt = new DateTime(2026, 11, 10, 12, 0, 0);

        var entity = new GisMtJournalEntity
        {
            Id = "sent-1",
            Kind = GisMtJournalKinds.LkReceipt,
            Status = GisMtJournalStatuses.Sent,
            Inn = "2464122182",
            ProductGroup = "water",
            SentAt = sentAt,
            CrptStatus = GisMtCrptStatuses.CheckedNotOk,
            CreatedAt = sentAt.AddMinutes(-1),
            Lines = [new GisMtJournalLine { Cis = "0104603334003509215ABC" }]
        };

        context.Database.ExecuteSqlRaw(
            """INSERT INTO "GisMtJournal" ("Id","Json","Kind","Status","CreatedAt") VALUES ('sent-1', {0}, 'LK_RECEIPT', 'sent', '2026-11-10 11:59:00')""",
            JsonSerializer.Serialize(entity, JsonSerializerOptions.Web));

        GisMtJournalSchema.Upgrade(context);

        var row = context.GisMtJournal.Single();
        Assert.Equal("2464122182", row.Inn);
        Assert.Equal("water", row.ProductGroup);
        Assert.Equal(GisMtCrptStatuses.CheckedNotOk, row.CrptStatus);
        Assert.Equal(sentAt, row.SentAt);
    }

    [Fact]
    public void Повторный_апгрейд_журнала_ничего_не_ломает()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SqliteDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new SqliteDbContext(options);
        context.Database.EnsureCreated();

        GisMtJournalSchema.Upgrade(context);
        GisMtJournalSchema.Upgrade(context);

        Assert.Empty(context.GisMtJournal);
    }

    [Fact]
    public void Uid_без_ключа_идемпотентности_в_уникальный_индекс_не_попадает()
    {
        var row = new GisMtJournalRow();
        var entity = new GisMtJournalEntity
        {
            Id = "1",
            Kind = GisMtJournalKinds.LkReceipt,
            DocDate = new DateOnly(2026, 11, 2),
            CreatedAt = DateTime.Now
        };

        GisMtJournalRowMapper.Fill(row, entity, "1");

        Assert.Null(row.Uid);
        Assert.Equal(GisMtJournalKinds.LkReceipt, row.Kind);
    }

    [Fact]
    public void Uid_постановки_на_кран_пишется_в_колонку()
    {
        var row = new GisMtJournalRow();
        var entity = new GisMtJournalEntity
        {
            Id = "2",
            Kind = GisMtJournalKinds.ConnectTap,
            Uid = " uid-1 ",
            DocDate = new DateOnly(2026, 11, 2),
            CreatedAt = DateTime.Now
        };

        GisMtJournalRowMapper.Fill(row, entity, "2");

        Assert.Equal(GisMtJournalKinds.ConnectTap, row.Kind);
        Assert.False(string.IsNullOrWhiteSpace(row.Uid));
    }
}
