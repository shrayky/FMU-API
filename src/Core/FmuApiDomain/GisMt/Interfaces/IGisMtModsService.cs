using CSharpFunctionalExtensions;
using FmuApiDomain.GisMt.Models;

namespace FmuApiDomain.GisMt.Interfaces;

/// <summary>
/// Места осуществления деятельности организации для выбора КПП и ФИАС.
/// </summary>
public interface IGisMtModsService
{
    Task<Result<IReadOnlyList<GisMtModInfo>>> Places(string inn, CancellationToken cancellationToken = default);
}
