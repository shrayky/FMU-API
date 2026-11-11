using CSharpFunctionalExtensions;

namespace FmuApiDomain.GisMt.Interfaces;

/// <summary>
/// Создание документа в ГИС МТ через lk/documents/create.
/// </summary>
public interface IGisMtLkDocumentsClient
{
    Task<Result<string>> CreateDocument(
        string token,
        string productGroup,
        string type,
        string productDocument,
        string signature,
        CancellationToken cancellationToken = default);
}
