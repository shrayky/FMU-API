using FmuApiDomain.Documents.Entities;

namespace FmuApiDomain.Documents.Interfaces;

/// <summary>
/// Файловая очередь документов при недоступности CouchDB.
/// </summary>
public interface IOfflineDocumentStore
{
    Task Save(RequestDocument document, string status);

    Task<OfflineDocumentRecord?> Get(string uid);

    Task Delete(string uid);

    Task<IReadOnlyList<OfflineDocumentRecord>> ListPending();
}
