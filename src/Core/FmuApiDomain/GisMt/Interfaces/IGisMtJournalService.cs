using CSharpFunctionalExtensions;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Models;

namespace FmuApiDomain.GisMt.Interfaces;

/// <summary>
/// Журнал исходящих документов ГИС МТ: создание, список, отправка.
/// </summary>
public interface IGisMtJournalService
{
    Task<Result<GisMtJournalPage>> List(string kind, string status, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<GisMtJournalEntity?> Get(string id);

    Task<Result<GisMtJournalEntity>> Create(GisMtJournalEntity document);

    Task<Result<GisMtJournalEntity>> Send(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Сверка акта, чья отправка не подтвердилась: найденный в Честном знаке документ привязывается к акту.
    /// </summary>
    Task<Result<GisMtJournalEntity>> ReconcileUnconfirmed(
        string id,
        string crptDocumentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Сверка не нашла документ в Честном знаке: акт возвращается в ошибку, отправку можно повторить.
    /// </summary>
    Task<Result<GisMtJournalEntity>> ReturnUnconfirmedToError(string id);

    /// <summary>
    /// Запрашивает в ГИС МТ статус обработки отправленного документа и сохраняет его в акте.
    /// </summary>
    Task<Result<GisMtJournalEntity>> CheckCrptStatus(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Опрашивает статусы всех отправленных актов, ожидающих обработки; возвращает число проверенных.
    /// </summary>
    Task<Result<int>> CheckPendingCrptStatuses(CancellationToken cancellationToken = default);

    Task<Result> Delete(string id);
}
