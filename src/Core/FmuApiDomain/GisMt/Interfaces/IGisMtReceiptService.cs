using CSharpFunctionalExtensions;
using FmuApiDomain.GisMt.Models;

namespace FmuApiDomain.GisMt.Interfaces;

/// <summary>
/// Создание акта списания весовой молочной продукции из списка марок.
/// </summary>
public interface IGisMtReceiptService
{
    Task<Result<GisMtReceiptCreateResult>> Create(string inn, IReadOnlyList<string> marks, CancellationToken cancellationToken = default);

    Task<Result<GisMtReceiptCreateResult>> Update(string id, string inn, IReadOnlyList<string> marks, CancellationToken cancellationToken = default);

    Task<Result<List<GisMtReceiptLinePreview>>> Preview(string inn, IReadOnlyList<string> marks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Создаёт черновик списания по истечении срока годности из марок остатков ГИС МТ.
    /// </summary>
    Task<Result<GisMtReceiptCreateResult>> CreateExpiration(string inn, string productGroup, IReadOnlyList<GisMtExpiredLine> lines, CancellationToken cancellationToken = default);

    /// <summary>
    /// Заменяет строки черновика списания по истечении срока годности.
    /// </summary>
    Task<Result<GisMtReceiptCreateResult>> UpdateExpiration(string id, string inn, string productGroup, IReadOnlyList<GisMtExpiredLine> lines, CancellationToken cancellationToken = default);

    /// <summary>
    /// Товарные группы организации, по которым есть остатки марок.
    /// </summary>
    Task<Result<List<string>>> Groups(string inn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Сколько у организации просроченных непроданных марок в товарной группе.
    /// </summary>
    Task<Result<int>> ExpiredCount(string inn, string productGroup, CancellationToken cancellationToken = default);

    /// <summary>
    /// Подбирает просроченные непроданные марки одной товарной группы для списания по сроку годности.
    /// </summary>
    Task<Result<GisMtExpiredSelection>> FillExpired(string inn, string productGroup, CancellationToken cancellationToken = default);
}
