using CSharpFunctionalExtensions;
using FmuApiDomain.GisMt.Models;

namespace FmuApiDomain.GisMt.Interfaces;

/// <summary>
/// Загрузка остатка марок из ГИС МТ.
/// </summary>
public interface IGisMtStockLoadService
{
    /// <summary>
    /// Загружает марки со статусом «В обороте» (INTRODUCED) для организации.
    /// </summary>
    Task<Result<GisMtStockLoadResult>> Load(string inn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Загружает остатки для всех организаций с включённой интеграцией ГИС МТ.
    /// </summary>
    Task<Result<GisMtStockLoadAllResult>> LoadAll(CancellationToken cancellationToken = default);
}
