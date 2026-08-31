using FmuApiDomain.Documents;

namespace FmuApiApplication.Documents.Interfaces;

/// <summary>
/// Проставляет статусы марок по закрытому документу Frontol.
/// </summary>
public interface IFrontolDocumentMarkStateService
{
    Task ApplyAsync(RequestDocument beginDocument);
}
