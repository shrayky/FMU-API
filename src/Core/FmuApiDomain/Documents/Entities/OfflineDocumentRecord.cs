namespace FmuApiDomain.Documents.Entities;

/// <summary>
/// Локальная копия документа Frontol для отложенной записи в CouchDB.
/// </summary>
public class OfflineDocumentRecord
{
    public RequestDocument Document { get; set; } = new();
    public string Status { get; set; } = OfflineDocumentStatus.Begun;
}
