using CSharpFunctionalExtensions;
using FmuApiDomain.GisMt.Models;

namespace FmuApiDomain.GisMt.Interfaces;

/// <summary>
/// Создание акта списания из списка марок: фасовка считает вес и цену, истечение срока — нет.
/// </summary>
public interface IGisMtReceiptService
{
    Task<Result<GisMtReceiptCreateResult>> Create(string inn, IReadOnlyList<string> marks, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Создаёт черновик по маркам со значениями с ТСД: количество и цена важнее расчётных.
    /// </summary>
    Task<Result<GisMtReceiptCreateResult>> CreateWithValues(string inn, IReadOnlyList<GisMtReceiptMark> marks, string? reason = null, int? printGroupId = null, string? uid = null, string? number = null, string? terminalId = null, CancellationToken cancellationToken = default);

    Task<Result<GisMtReceiptCreateResult>> Update(string id, string inn, IReadOnlyList<string> marks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Правит черновик. Заполненные количество и цена остаются, пустые берутся из расчёта.
    /// </summary>
    Task<Result<GisMtReceiptCreateResult>> UpdateWithValues(string id, string inn, IReadOnlyList<GisMtReceiptMark> marks, CancellationToken cancellationToken = default);

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
