using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Options.Organization;

namespace FmuApiDomain.GisMt.Interfaces;

/// <summary>
/// Сохранение сведений о КИ из cises/info в БД остатка.
/// </summary>
public interface IGisMtCisInfoSaver
{
    /// <summary>
    /// Запрашивает cises/info пачками и сохраняет марки.
    /// </summary>
    Task<Result<int>> SaveBatches(
        PrintGroupData organisation,
        string token,
        string productGroup,
        IReadOnlyList<string> cises,
        string sourceDocumentId,
        CancellationToken cancellationToken = default);
}
