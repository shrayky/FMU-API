using CSharpFunctionalExtensions;

namespace FmuApiDomain.GisMt.Interfaces;

/// <summary>
/// HTTP-клиент TrueAPI: вложения агрегатов (упаковок).
/// </summary>
public interface IGisMtCisAggregationClient
{
    /// <summary>
    /// Вложения первого слоя для списка упаковок: код упаковки → коды вложений.
    /// </summary>
    Task<Result<Dictionary<string, List<string>>>> AggregatedList(
        string token,
        IReadOnlyList<string> cises,
        string? productGroup,
        CancellationToken cancellationToken = default);
}
