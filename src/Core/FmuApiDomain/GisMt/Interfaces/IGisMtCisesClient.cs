using CSharpFunctionalExtensions;
using FmuApiDomain.GisMt.Models;
using FmuApiDomain.TrueApi.ProductInfo;

namespace FmuApiDomain.GisMt.Interfaces;

/// <summary>
/// HTTP-клиент TrueAPI для сведений о КИ и карточек товаров.
/// </summary>
public interface IGisMtCisesClient
{
    /// <summary>
    /// Получает общедоступную информацию о КИ.
    /// </summary>
    Task<Result<List<CisInfoResponseItem>>> CisesInfo(
        string token,
        IReadOnlyList<string> cises,
        string? productGroup,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ищет КИ по фильтрам (остаток / статусы).
    /// </summary>
    Task<Result<CisSearchResponse>> SearchCises(
        string token,
        CisSearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает информацию о товарах по списку GTIN.
    /// </summary>
    Task<Result<ProductsInformationTrueApi>> ProductInfo(
        string token,
        List<string> gtins,
        CancellationToken cancellationToken = default);
}
