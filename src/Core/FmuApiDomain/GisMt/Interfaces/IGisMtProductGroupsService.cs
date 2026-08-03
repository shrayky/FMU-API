using CSharpFunctionalExtensions;

namespace FmuApiDomain.GisMt.Interfaces;

/// <summary>
/// Управление товарными группами организации в ГИС МТ.
/// </summary>
public interface IGisMtProductGroupsService
{
    /// <summary>
    /// Загружает ProductGroups из /participants и сохраняет в конфигурацию.
    /// </summary>
    Task<Result<IReadOnlyList<string>>> Refresh(string inn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает сохранённые группы; при отсутствии — загружает из ГИС МТ.
    /// </summary>
    Task<Result<IReadOnlyList<string>>> GetOrRefresh(string inn, CancellationToken cancellationToken = default);
}
