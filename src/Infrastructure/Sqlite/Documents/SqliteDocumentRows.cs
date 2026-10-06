using System.Text.Json;

namespace Sqlite.Documents;

/// <summary>
/// Строка таблицы SQLite: идентификатор, тело сущности в JSON и колонки для выборки.
/// </summary>
public interface ISqliteDocumentRow
{
    string Id { get; set; }

    string Json { get; set; }
}

/// <summary>
/// Документ CouchDB, подготовленный к записи в таблицу SQLite.
/// </summary>
public sealed record CouchDbImportDocument(string Id, JsonElement Data);

/// <summary>
/// Строка справочника марок.
/// </summary>
public class MarkRow : ISqliteDocumentRow
{
    public string Id { get; set; } = string.Empty;
    public string Json { get; set; } = string.Empty;
    public string MarkId { get; set; } = string.Empty;
    public long ReqTimestamp { get; set; }
}

/// <summary>
/// Строка документов Frontol.
/// </summary>
public class DocumentRow : ISqliteDocumentRow
{
    public string Id { get; set; } = string.Empty;
    public string Json { get; set; } = string.Empty;
}

/// <summary>
/// Строка статистики проверок марок.
/// </summary>
public class MarkCheckingStatisticRow : ISqliteDocumentRow
{
    public string Id { get; set; } = string.Empty;
    public string Json { get; set; } = string.Empty;
    public string SGtin { get; set; } = string.Empty;
    public long CheckDay { get; set; }
    public DateTime CheckDate { get; set; }
}

/// <summary>
/// Строка пивных кранов.
/// </summary>
public class BeerOnTapRow : ISqliteDocumentRow
{
    public string Id { get; set; } = string.Empty;
    public string Json { get; set; } = string.Empty;
}

/// <summary>
/// Строка загруженных документов ГИС МТ.
/// </summary>
public class GisMtDocumentRow : ISqliteDocumentRow
{
    public string Id { get; set; } = string.Empty;
    public string Json { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public DateTime LoadedAt { get; set; }
}

/// <summary>
/// Строка остатков марок ГИС МТ.
/// </summary>
public class GisMtMarkRow : ISqliteDocumentRow
{
    public string Id { get; set; } = string.Empty;
    public string Json { get; set; } = string.Empty;
    public string SGtin { get; set; } = string.Empty;
    public string Cis { get; set; } = string.Empty;
    public string ProductGroup { get; set; } = string.Empty;
    public DateTime InfoLoadedAt { get; set; }
    public bool Sold { get; set; }
    public DateTime? ExpireDate { get; set; }
}

/// <summary>
/// Строка каталога GTIN.
/// </summary>
public class GtinCatalogRow : ISqliteDocumentRow
{
    public string Id { get; set; } = string.Empty;
    public string Json { get; set; } = string.Empty;
    public string Gtin { get; set; } = string.Empty;
}

/// <summary>
/// Строка хода переноса: ключ — имя базы CouchDB.
/// </summary>
public class ImportProgressRow : ISqliteDocumentRow
{
    public string Id { get; set; } = string.Empty;
    public string Json { get; set; } = string.Empty;
    public string LastId { get; set; } = string.Empty;
    public bool Completed { get; set; }
}
