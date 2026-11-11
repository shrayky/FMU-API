using CSharpFunctionalExtensions;
using FmuApiDomain.GisMt.Models;

namespace FmuApiDomain.GisMt.Interfaces;

/// <summary>
/// HTTP-клиент TrueAPI: положение кода идентификации и его упаковка.
/// </summary>
public interface IGisMtCisHistoryClient
{
    Task<Result<List<GisMtCisHistoryRecord>>> History(
        string token,
        string cis,
        CancellationToken cancellationToken = default);
}
