using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration.Options.Organization;
using FmuApiDomain.GisMt;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Models;
using FmuApiDomain.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace FmuApiApplication.GisMt;

/// <summary>
/// Сохранение сведений о КИ из cises/info в БД остатка.
/// </summary>
[AutoRegisterService(ServiceLifetime.Scoped)]
public class GisMtCisInfoSaver(
    IGisMtCisesClient cisesClient,
    IGisMtMarkRepository markRepository) : IGisMtCisInfoSaver
{
    /// <summary>
    /// Максимальный размер пакета для cises/info (лимит TrueAPI).
    /// </summary>
    public const int BatchSize = 1000;

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

        if (entities.Count == 0)
            return Result.Success(0);

        if (!await _markRepository.SaveRange(entities))
            return Result.Failure<int>("Ошибка сохранения марок в CouchDB");

        return Result.Success(entities.Count);
    }
}
