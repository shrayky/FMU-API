namespace Sqlite.Documents;

/// <summary>
/// Таблица SQLite, в которую перенос из CouchDB дописывает документы.
/// </summary>
public interface ISqliteImportTarget
{
    /// <summary>
    /// Имя базы CouchDB, документы которой попадают в эту таблицу.
    /// </summary>
    string DatabaseName { get; }

    /// <summary>
    /// Записывает документы, строк с такими Id ещё нет. Возвращает число записанных строк.
    /// </summary>
    Task<int> InsertMissing(IReadOnlyList<CouchDbImportDocument> documents, CancellationToken cancellationToken);
}
