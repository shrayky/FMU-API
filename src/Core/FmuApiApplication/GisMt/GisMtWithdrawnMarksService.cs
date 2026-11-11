using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration.Options.Organization;
using FmuApiDomain.GisMt;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.GisMt;

/// <summary>
/// Помечает проданными марки, которых не было в свежем остатке, по данным cises/info.
/// </summary>
[AutoRegisterService(ServiceLifetime.Scoped)]
public class GisMtWithdrawnMarksService(
    ILogger<GisMtWithdrawnMarksService> logger,
    IGisMtCisesClient cisesClient,
    IGisMtMarkRepository markRepository) : IGisMtWithdrawnMarksService
{
    private const int BatchSize = 1000;
    private const int MaxBatches = 50;

    private readonly ILogger<GisMtWithdrawnMarksService> _logger = logger;
    private readonly IGisMtCisesClient _cisesClient = cisesClient;
    private readonly IGisMtMarkRepository _markRepository = markRepository;

    public async Task<int> Reconcile(
        PrintGroupData organisation,
        string token,
        string productGroup,
        IReadOnlySet<string> freshCodes,
        DateTime loadStartedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var withdrawn = 0;

        for (var iteration = 0; iteration < MaxBatches; iteration++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var candidates = await _markRepository.GetUnsoldLoadedBefore(
                organisation.INN,
                productGroup,
                loadStartedAtUtc,
                BatchSize);
            if (candidates.Count == 0)
                break;

            var batch = new List<GisMtMarkEntity>();
            var fresh = new List<GisMtMarkEntity>();
            var loadedAt = DateTime.UtcNow;

            foreach (var mark in candidates)
            {
                if (freshCodes.Contains(mark.Cis) || freshCodes.Contains(mark.SGtin))
                    fresh.Add(mark);
                else
                    batch.Add(mark);
            }

            // Марки свежего остатка не перепроверяем, но отмечаем датой сверки:
            // иначе они навсегда занимают начало выборки, и выбывшие марки за ними не доходят до cises/info.
            foreach (var mark in fresh)
                mark.InfoLoadedAt = loadedAt;

            if (batch.Count > 0)
            {
                var info = await _cisesClient.CisesInfo(
                    token,
                    batch.Select(mark => mark.Cis).ToList(),
                    productGroup,
                    cancellationToken);

                if (info.IsFailure)
                {
                    // Свежие марки этой страницы уже получили дату сверки в памяти: сохраняем их,
                    // иначе следующая сверка снова начнётся с них же.
                    await SaveFresh(fresh, productGroup);
                    _logger.LogWarning("Сверка статусов марок группы {Group}: {Error}", productGroup, info.Error);
                    break;
                }

                withdrawn += ApplyInfo(batch, info.Value, loadedAt);
            }

            if (!await _markRepository.SaveRange(candidates))
                _logger.LogWarning("Сверка статусов марок группы {Group}: не удалось сохранить марки", productGroup);
        }

        if (withdrawn > 0)
            _logger.LogInformation("Сверка остатка группы {Group}: выбывшими из оборота помечено {Count} марок", productGroup, withdrawn);

        return withdrawn;
    }

    /// <summary>
    /// Сохраняет даты сверки марок свежего остатка, чтобы они не занимали начало следующей выборки.
    /// </summary>
    private async Task SaveFresh(IReadOnlyList<GisMtMarkEntity> fresh, string productGroup)
    {
        if (fresh.Count == 0)
            return;

        if (!await _markRepository.SaveRange(fresh))
            _logger.LogWarning(
                "Сверка статусов марок группы {Group}: не удалось сохранить даты сверки свежих марок",
                productGroup);
    }

    /// <summary>
    /// Проставляет признак продажи по свежим сведениям и обновляет дату загрузки, чтобы марка не проверялась повторно.
    /// </summary>
    private static int ApplyInfo(
        IReadOnlyList<GisMtMarkEntity> marks,
        IReadOnlyList<CisInfoResponseItem> info,
        DateTime loadedAt)
    {
        var bySgtin = new Dictionary<string, CisInfoData>(StringComparer.Ordinal);

        foreach (var item in info)
        {
            if (item.CisInfo is null || !string.IsNullOrEmpty(item.ErrorCode))
                continue;

            var sgtin = GisMtMarkMapper.Sgtin(item.CisInfo);
            if (!string.IsNullOrEmpty(sgtin))
                bySgtin[sgtin] = item.CisInfo;
        }

        var withdrawn = 0;

        foreach (var mark in marks)
        {
            if (bySgtin.TryGetValue(mark.SGtin, out var cisInfo)
                && GisMtMarkMapper.ApplyCisInfo(mark, cisInfo))
                withdrawn++;

            mark.InfoLoadedAt = loadedAt;
        }

        return withdrawn;
    }
}
