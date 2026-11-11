using CSharpFunctionalExtensions;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Models;

namespace FmuApiDomain.GisMt.Interfaces;

/// <summary>
/// Хранение актов журнала исходящих документов ГИС МТ.
/// </summary>
public interface IGisMtJournalRepository
{
    Task<GisMtJournalEntity?> Get(string id);

    Task<GisMtJournalEntity?> FindByUid(string uid);

    Task<bool> Save(GisMtJournalEntity entity);

    /// <summary>
    /// Записывает акт, только если в хранилище у него ещё ожидаемый статус:
    /// два параллельных вызова не должны оба начать отправку.
    /// </summary>
    Task<bool> TryUpdateStatus(GisMtJournalEntity entity, string expectedStatus);

    Task<bool> Delete(string id);

    Task<Result<GisMtJournalPage>> List(string kind, string status, int page, int pageSize);

    /// <summary>
    /// Отправленные акты, у которых статус в ГИС МТ ещё не окончательный: проверялись раньше указанного момента,
    /// отправлены не раньше указанной даты.
    /// </summary>
    Task<List<GisMtJournalEntity>> GetPendingCrptStatus(DateTime checkedBefore, DateTime sentAfter, int limit);

    /// <summary>
    /// КИ, которые уже уходили в ГИС МТ в отправленных актах этой организации и товарной группы.
    /// Ошибка чтения не подменяется пустым набором: пустой набор значит, что таких КИ нет.
    /// </summary>
    Task<Result<HashSet<string>>> GetSubmittedCis(string organisationInn, string productGroup, DateTime sentAfter, int limit);
}
