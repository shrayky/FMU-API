namespace FmuApiDomain.Documents;

/// <summary>
/// Статус документа во файловой очереди до выгрузки в CouchDB.
/// </summary>
public static class OfflineDocumentStatus
{
    public const string Begun = "begun";
    public const string Committed = "committed";
    public const string Cancelled = "cancelled";
}
