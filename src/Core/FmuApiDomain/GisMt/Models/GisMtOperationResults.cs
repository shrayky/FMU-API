namespace FmuApiDomain.GisMt.Models;

public record GisMtDocumentsSyncResult(
    int OrganisationsProcessed,
    int DocumentsLoaded,
    int MarksSaved,
    int MarksDeleted,
    IReadOnlyList<string> Errors);

public record GisMtStockLoadResult(
    int MarksSaved,
    IReadOnlyList<string> Errors);

public record GisMtStockLoadAllResult(
    int OrganisationsProcessed,
    int MarksSaved,
    IReadOnlyList<string> Errors);
