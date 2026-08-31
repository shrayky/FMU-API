namespace FmuApiApplication.Documents.Interfaces;

/// <summary>
/// Выгружает файловую очередь документов в CouchDB.
/// </summary>
public interface IOfflineDocumentFlushService
{
    Task FlushAsync(CancellationToken cancellationToken);
}
