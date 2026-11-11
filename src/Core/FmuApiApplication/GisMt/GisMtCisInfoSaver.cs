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
/// Сохранение сведений о КИ из cises/info в БД остатка.
/// </summary>
[AutoRegisterService(ServiceLifetime.Scoped)]
public class GisMtCisInfoSaver(
    ILogger<GisMtCisInfoSaver> logger,
    IGisMtCisesClient cisesClient,
    IGisMtMarkRepository markRepository) : IGisMtCisInfoSaver
{
    /// <summary>
    /// Максимальный размер пакета для cises/info (лимит TrueAPI).
    /// </summary>
    public const int BatchSize = 1000;

    private readonly ILogger<GisMtCisInfoSaver> _logger = logger;
    private readonly IGisMtCisesClient _cisesClient = cisesClient;
    private readonly IGisMtMarkRepository _markRepository = markRepository;

    /// <summary>
    /// Запрашивает cises/info пачками и сохраняет марки.
    /// </summary>
    public async Task<Result<int>> SaveBatches(
        PrintGroupData organisation,
        string token,
        string productGroup,
        IReadOnlyList<string> cises,
        string sourceDocumentId,
        CancellationToken cancellationToken = default)
    {
        var marksSaved = 0;

        foreach (var batch in cises.Chunk(BatchSize))
        {
            var saved = await SaveBatch(
                organisation,
                token,
                productGroup,
                batch.ToList(),
                sourceDocumentId,
                cancellationToken);

            if (saved.IsFailure)
                return saved;

            marksSaved += saved.Value;
        }

        return Result.Success(marksSaved);
    }

    private async Task<Result<int>> SaveBatch(
        PrintGroupData organisation,
        string token,
        string productGroup,
        List<string> cises,
        string sourceDocumentId,
        CancellationToken cancellationToken)
    {
        var cisInfo = await _cisesClient.CisesInfo(
            token,
            cises,
            productGroup,
            cancellationToken);

        if (cisInfo.IsFailure)
            return Result.Failure<int>(cisInfo.Error);

        LogRejectedCodes(cisInfo.Value, sourceDocumentId);

        var loadedAt = DateTime.UtcNow;
        var entities = new List<GisMtMarkEntity>();
        foreach (var responseItem in cisInfo.Value)
        {
            if (responseItem.CisInfo is null || !string.IsNullOrEmpty(responseItem.ErrorCode))
                continue;

            entities.Add(GisMtMarkMapper.FromCisInfo(
                responseItem.CisInfo,
                organisation.INN,
                sourceDocumentId,
                loadedAt));
        }

        entities = entities
            .GroupBy(e => e.Id)
            .Select(g => g.Last())
            .ToList();

        if (entities.Count == 0)
            return Result.Success(0);

        if (sourceDocumentId == GisMtMarkSources.Stock)
            await KeepDocumentLinks(entities);

        if (!await _markRepository.SaveRange(entities))
            return Result.Failure<int>("Ошибка сохранения марок в CouchDB");

        return Result.Success(entities.Count);
    }

    private void LogRejectedCodes(IReadOnlyList<CisInfoResponseItem> items, string sourceDocumentId)
    {
        var rejected = items
            .Where(item => !string.IsNullOrEmpty(item.ErrorCode))
            .ToList();

        if (rejected.Count == 0)
            return;

        var first = rejected[0];

        _logger.LogWarning(
            "cises/info для {SourceDocumentId}: отклонено {Rejected} из {Total} кодов, первый {Cis}: {ErrorCode} {Error}",
            sourceDocumentId,
            rejected.Count,
            items.Count,
            first.CisInfo?.RequestedCis ?? first.CisInfo?.Cis,
            first.ErrorCode,
            first.ErrorMessage);
    }

    /// <summary>
    /// Загрузка остатков не стирает документ, из которого марка пришла.
    /// </summary>
    private async Task KeepDocumentLinks(List<GisMtMarkEntity> entities)
    {
        var existing = await _markRepository.GetListById(entities.Select(entity => entity.Id).ToList());

        if (existing.Count == 0)
            return;

        var documentsByMark = existing
            .Where(mark => !string.IsNullOrWhiteSpace(mark.SourceDocumentId))
            .GroupBy(mark => mark.Id)
            .ToDictionary(group => group.Key, group => group.Last().SourceDocumentId);

        foreach (var entity in entities)
        {
            if (documentsByMark.TryGetValue(entity.Id, out var documentId))
                entity.SourceDocumentId = documentId;
        }
    }
}
