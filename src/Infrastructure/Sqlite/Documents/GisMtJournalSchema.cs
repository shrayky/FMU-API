using FmuApiDomain.GisMt.Entities;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;

namespace Sqlite.Documents;

/// <summary>
/// Таблица журнала ГИС МТ: создаётся и в уже существующем файле, потому что Database.EnsureCreated новые таблицы не добавляет.
/// </summary>
public static class GisMtJournalSchema
{
    public const string TableName = "GisMtJournal";

    public const string InnColumn = "Inn";
    public const string ProductGroupColumn = "ProductGroup";
    public const string SentAtColumn = "SentAt";
    public const string CrptStatusColumn = "CrptStatus";

    public const string SubmittedQueryIndex = "IX_GisMtJournal_Inn_ProductGroup_Status";

    private const int BackfillBatchSize = 5000;

    public const string TableSql = """
        CREATE TABLE IF NOT EXISTS "GisMtJournal" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_GisMtJournal" PRIMARY KEY,
            "Json" TEXT NOT NULL,
            "Uid" TEXT NULL,
            "Kind" TEXT NOT NULL,
            "Status" TEXT NOT NULL,
            "CreatedAt" TEXT NOT NULL,
            "Inn" TEXT NOT NULL DEFAULT '',
            "ProductGroup" TEXT NOT NULL DEFAULT '',
            "SentAt" TEXT NULL,
            "CrptStatus" TEXT NOT NULL DEFAULT ''
        )
        """;

    public static readonly string[] IndexesSql =
    [
        """CREATE UNIQUE INDEX IF NOT EXISTS "IX_GisMtJournal_Uid" ON "GisMtJournal" ("Uid")""",
        """CREATE INDEX IF NOT EXISTS "IX_GisMtJournal_CreatedAt" ON "GisMtJournal" ("CreatedAt")""",
        """CREATE INDEX IF NOT EXISTS "IX_GisMtJournal_Kind_Status" ON "GisMtJournal" ("Kind", "Status")""",
        $"""CREATE INDEX IF NOT EXISTS "{SubmittedQueryIndex}" ON "{TableName}" ("Inn", "ProductGroup", "Status", "SentAt")"""
    ];

    /// <summary>
    /// Догоняет схему журнала: в уже существующей таблице колонок выбора КИ ещё нет.
    /// </summary>
    public static void Upgrade(SqliteDbContext context)
    {
        EnsureColumn(context, InnColumn, "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(context, ProductGroupColumn, "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(context, SentAtColumn, "TEXT NULL");
        EnsureColumn(context, CrptStatusColumn, "TEXT NOT NULL DEFAULT ''");

        context.Database.ExecuteSqlRaw(
            $"""CREATE INDEX IF NOT EXISTS "{SubmittedQueryIndex}" ON "{TableName}" ("{InnColumn}", "{ProductGroupColumn}", "Status", "{SentAtColumn}")""");

        Backfill(context);
    }

    private static void EnsureColumn(SqliteDbContext context, string column, string definition)
    {
        if (HasColumn(context, column))
            return;

        Execute(context, $"""ALTER TABLE "{TableName}" ADD COLUMN "{column}" {definition}""");
    }

    private static void Execute(SqliteDbContext context, string sql)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
            connection.Open();

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
        finally
        {
            if (shouldClose)
                connection.Close();
        }
    }

    private static bool HasColumn(SqliteDbContext context, string column)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
            connection.Open();

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info(\"{TableName}\")";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
        finally
        {
            if (shouldClose)
                connection.Close();
        }
    }

    /// <summary>
    /// Заполняет добавленные колонки из JSON уже сохранённых актов.
    /// </summary>
    private static void Backfill(SqliteDbContext context)
    {
        BackfillText(
            context,
            row => row.Inn == string.Empty,
            entity => entity.Inn,
            (row, value) => row.Inn = value);

        BackfillText(
            context,
            row => row.ProductGroup == string.Empty,
            entity => entity.ProductGroup,
            (row, value) => row.ProductGroup = value);

        BackfillText(
            context,
            row => row.CrptStatus == string.Empty,
            entity => entity.CrptStatus,
            (row, value) => row.CrptStatus = value);

        BackfillSentAt(context);
    }

    private static void BackfillText(
        SqliteDbContext context,
        System.Linq.Expressions.Expression<Func<GisMtJournalRow, bool>> isEmpty,
        Func<GisMtJournalEntity, string> value,
        Action<GisMtJournalRow, string> assign)
    {
        while (true)
        {
            var rows = context.GisMtJournal
                .Where(isEmpty)
                .Take(BackfillBatchSize)
                .ToList();

            if (rows.Count == 0)
                return;

            var filled = 0;

            foreach (var row in rows)
            {
                var entity = Deserialize(row);
                if (entity == null)
                    continue;

                var text = value(entity);
                if (string.IsNullOrEmpty(text))
                    continue;

                assign(row, text);
                filled++;
            }

            if (filled == 0)
                return;

            context.SaveChanges();
        }
    }

    private static void BackfillSentAt(SqliteDbContext context)
    {
        while (true)
        {
            var rows = context.GisMtJournal
                .Where(row => row.SentAt == null)
                .Take(BackfillBatchSize)
                .ToList();

            if (rows.Count == 0)
                return;

            var filled = 0;

            foreach (var row in rows)
            {
                var entity = Deserialize(row);
                if (entity?.SentAt == null)
                    continue;

                row.SentAt = entity.SentAt;
                filled++;
            }

            if (filled == 0)
                return;

            context.SaveChanges();
        }
    }

    private static GisMtJournalEntity? Deserialize(GisMtJournalRow row) =>
        JsonSerializer.Deserialize<GisMtJournalEntity>(row.Json, JsonSerializerOptions.Web);
}

/// <summary>
/// Заполнение колонок строки журнала из сущности.
/// </summary>
public static class GisMtJournalRowMapper
{
    public static void Fill(GisMtJournalRow row, GisMtJournalEntity entity, string id)
    {
        row.Id = id;
        row.Uid = string.IsNullOrWhiteSpace(entity.Uid) ? null : entity.Uid;
        row.Kind = entity.Kind;
        row.Status = entity.Status;
        row.CreatedAt = entity.CreatedAt;
        row.Inn = entity.Inn ?? string.Empty;
        row.ProductGroup = entity.ProductGroup ?? string.Empty;
        row.SentAt = entity.SentAt;
        row.CrptStatus = entity.CrptStatus ?? string.Empty;
    }
}
