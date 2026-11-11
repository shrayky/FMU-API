using CSharpFunctionalExtensions;
using FmuApiDomain.GisMt.Models;

namespace FmuApiDomain.GisMt.Interfaces;

/// <summary>
/// Места осуществления деятельности организации в ГИС МТ.
/// </summary>
public interface IGisMtModsClient
{
    Task<Result<List<GisMtModInfo>>> Places(string token, string inn, CancellationToken cancellationToken = default);
}
