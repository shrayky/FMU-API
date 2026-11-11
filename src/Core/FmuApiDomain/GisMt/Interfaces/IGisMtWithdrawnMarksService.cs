using FmuApiDomain.Configuration.Options.Organization;

namespace FmuApiDomain.GisMt.Interfaces;

/// <summary>
/// Сверка марок, которые ЧЗ считает выбывшими, но которые остались в остатке со старым признаком продажи.
/// </summary>
public interface IGisMtWithdrawnMarksService
{
    /// <summary>
    /// Перечитывает сведения о марках группы, которых не было в свежем остатке, и помечает выбывшие проданными.
    /// </summary>
    /// <returns>Число марок, помеченных проданными.</returns>
    Task<int> Reconcile(
        PrintGroupData organisation,
        string token,
        string productGroup,
        IReadOnlySet<string> freshCodes,
        DateTime loadStartedAtUtc,
        CancellationToken cancellationToken = default);
}
