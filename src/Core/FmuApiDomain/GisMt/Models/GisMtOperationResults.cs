using FmuApiDomain.GisMt.Entities;

namespace FmuApiDomain.GisMt.Models;

public record GisMtDocumentsSyncResult(
    int OrganisationsProcessed,
    int DocumentsLoaded,
    int MarksSaved,
    int MarksDeleted,
    IReadOnlyList<string> Errors);

public record GisMtStockLoadResult(
    int MarksSaved,
    int MarksWithdrawn,
    IReadOnlyList<string> Errors);

public record GisMtStockLoadAllResult(
    int OrganisationsProcessed,
    int MarksSaved,
    int MarksWithdrawn,
    IReadOnlyList<string> Errors);

/// <summary>
/// Итог создания акта списания: документ и ошибки строк, которые в него не попали.
/// </summary>
public record GisMtReceiptCreateResult(
    GisMtJournalEntity Document,
    IReadOnlyList<string> LineErrors);
