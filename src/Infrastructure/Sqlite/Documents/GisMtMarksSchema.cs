using FmuApiDomain.GisMt.Entities;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq.Expressions;
using System.Text.Json;

namespace Sqlite.Documents;

/// <summary>
/// Догоняет схему остатков ГИС МТ: Database.EnsureCreated не добавляет колонки в уже существующую таблицу.
/// </summary>
public static class GisMtMarksSchema
{
    public const string TableName = "GisMtMarks";
    public const string OrganisationInnColumn = "OrganisationInn";
    public const string SourceDocumentColumn = "SourceDocumentId";
    public const string OrganisationInnIndex = "IX_GisMtMarks_OrganisationInn_ProductGroup_Sold";
    public const string SourceDocumentIndex = "IX_GisMtMarks_SourceDocumentId";

    private const int BackfillBatchSize = 5000;

    public static void Upgrade(SqliteDbContext context)
    {
        EnsureColumn(context, OrganisationInnColumn);
        EnsureColumn(context, SourceDocumentColumn);

        context.Database.ExecuteSqlRaw(
            $"""CREATE INDEX IF NOT EXISTS "{OrganisationInnIndex}" ON "{TableName}" ("{OrganisationInnColumn}", "ProductGroup", "Sold")""");

        context.Database.ExecuteSqlRaw(
            $"""CREATE INDEX IF NOT EXISTS "{SourceDocumentIndex}" ON "{TableName}" ("{SourceDocumentColumn}")""");

        Backfill(
            context,
            row => row.OrganisationInn == string.Empty,
            entity => entity.OrganisationInn,
            (row, value) => row.OrganisationInn = value);

        Backfill(
            context,
            row => row.SourceDocumentId == string.Empty,
            entity => entity.SourceDocumentId,
            (row, value) => row.SourceDocumentId = value);
    }

    private static void EnsureColumn(SqliteDbContext context, string column)
    {
        if (HasColumn(context, column))
            return;

        context.Database.ExecuteSqlRaw(
            $"""ALTER TABLE "{TableName}" ADD COLUMN "{column}" TEXT NOT NULL DEFAULT ''""");
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
    /// Заполняет добавленную колонку из JSON уже сохранённых марок.
    /// </summary>
    private static void Backfill(
        SqliteDbContext context,
        Expression<Func<GisMtMarkRow, bool>> isEmpty,
        Func<GisMtMarkEntity, string> value,
        Action<GisMtMarkRow, string> assign)
    {
        while (true)
        {
            var rows = context.GisMtMarks
                .Where(isEmpty)
                .Take(BackfillBatchSize)
                .ToList();

            if (rows.Count == 0)
                return;

            var filled = 0;

            foreach (var row in rows)
            {
                var entity = JsonSerializer.Deserialize<GisMtMarkEntity>(row.Json, JsonSerializerOptions.Web);
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
}
