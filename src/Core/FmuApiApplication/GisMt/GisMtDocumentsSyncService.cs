using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options.Organization;
using FmuApiDomain.GisMt;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Models;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.GisMt;

/// <summary>
/// Синхронизация входящих документов УПД/УКД из ГИС МТ.
/// </summary>
[AutoRegisterService(ServiceLifetime.Scoped)]
public class GisMtDocumentsSyncService(
    ILogger<GisMtDocumentsSyncService> logger,
    IParametersService parametersService,
    IApplicationState applicationState,
    IGisMtDocumentsClient documentsClient,
    IGisMtDocumentRepository documentRepository,
    IGisMtMarkRepository markRepository,
    IGisMtProductGroupsService productGroupsService,
    IGisMtCisInfoSaver cisInfoSaver) : IGisMtDocumentsSyncService
{
    private const int CleanupBatchSize = 1000;

    private readonly ILogger<GisMtDocumentsSyncService> _logger = logger;
    private readonly IParametersService _parametersService = parametersService;
    private readonly IApplicationState _applicationState = applicationState;
    private readonly IGisMtDocumentsClient _documentsClient = documentsClient;
    private readonly IGisMtDocumentRepository _documentRepository = documentRepository;
    private readonly IGisMtMarkRepository _markRepository = markRepository;
    private readonly IGisMtProductGroupsService _productGroupsService = productGroupsService;
    private readonly IGisMtCisInfoSaver _cisInfoSaver = cisInfoSaver;

    /// <summary>
    /// Синхронизирует входящие документы за настроенный период.
    /// </summary>
    public async Task<Result<GisMtDocumentsSyncResult>> Sync(CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var organisationsProcessed = 0;
        var documentsLoaded = 0;
        var marksSaved = 0;
        var marksDeleted = 0;

        try
        {
            var parameters = await _parametersService.CurrentAsync();
            var syncDays = parameters.GisMtSettings.DocumentsSyncDays;
            if (syncDays < 1)
                syncDays = 1;

            var periodEnd = DateTime.UtcNow;
            var periodStart = periodEnd.Date.AddDays(-(syncDays - 1));

            foreach (var organisation in parameters.OrganisationConfig.PrintGroups)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (!organisation.TrueApiIntegrationSettings.Enable || string.IsNullOrWhiteSpace(organisation.INN))
                    continue;

                var token = _applicationState.TrueApiToken(organisation.INN).Token;
                if (string.IsNullOrWhiteSpace(token))
                {
                    errors.Add($"Нет Bearer-токена для ИНН {organisation.INN}");
                    continue;
                }

                organisationsProcessed++;

                var groupsResult = await _productGroupsService.GetOrRefresh(organisation.INN, cancellationToken);
                if (groupsResult.IsFailure)
                {
                    errors.Add(groupsResult.Error);
                    continue;
                }

                foreach (var productGroup in groupsResult.Value)
                {
                    var syncGroup = await SyncOrganisationProductGroup(
                        organisation,
                        token,
                        productGroup,
                        periodStart,
                        periodEnd,
                        cancellationToken);

                    documentsLoaded += syncGroup.DocumentsLoaded;
                    marksSaved += syncGroup.MarksSaved;
                    errors.AddRange(syncGroup.Errors);
                }

                marksDeleted += await CleanupMarks(organisation.INN, parameters.GisMtSettings.MarkRetentionDays, cancellationToken);
            }

            return Result.Success(new GisMtDocumentsSyncResult(
                organisationsProcessed,
                documentsLoaded,
                marksSaved,
                marksDeleted,
                errors));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка синхронизации входящих документов ГИС МТ");
            return Result.Failure<GisMtDocumentsSyncResult>(ex.Message);
        }
    }

    private async Task<(int DocumentsLoaded, int MarksSaved, List<string> Errors)> SyncOrganisationProductGroup(
        PrintGroupData organisation,
        string token,
        string productGroup,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var documentsLoaded = 0;
        var marksSaved = 0;

        string? did = null;
        string? orderedColumnValue = null;
        var hasNext = true;

        while (hasNext && !cancellationToken.IsCancellationRequested)
        {
            var listResult = await _documentsClient.DocumentList(
                token,
                productGroup,
                organisation.INN,
                periodStart,
                periodEnd,
                GisMtDocumentTypes.UpdUkd,
                did,
                orderedColumnValue,
                cancellationToken);

            if (listResult.IsFailure)
            {
                errors.Add($"doc/list pg={productGroup}: {listResult.Error}");
                break;
            }

            var page = listResult.Value;
            var incoming = page.Results.Where(x => x.Input).ToList();

            foreach (var item in incoming)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (await _documentRepository.Exists(item.Number))
                    continue;

                var process = await ProcessDocument(organisation, token, productGroup, item, cancellationToken);
                if (process.IsFailure)
                {
                    errors.Add($"{item.Number}: {process.Error}");
                    continue;
                }

                documentsLoaded++;
                marksSaved += process.Value;
            }

            hasNext = page.NextPage && page.Results.Count > 0;
            if (hasNext)
            {
                var last = page.Results[^1];
                did = last.Number;
                orderedColumnValue = (last.DocDate ?? last.ReceivedAt ?? periodStart)
                    .ToUniversalTime()
                    .ToString("yyyy-MM-ddTHH:mm:ss.fff'Z'");
            }
        }

        return (documentsLoaded, marksSaved, errors);
    }

    private async Task<Result<int>> ProcessDocument(
        PrintGroupData organisation,
        string token,
        string productGroup,
        GisMtDocListItem item,
        CancellationToken cancellationToken)
    {
        var infoResult = await _documentsClient.DocumentInfo(
            token,
            item.Number,
            productGroup,
            cancellationToken);

        if (infoResult.IsFailure)
            return Result.Failure<int>(infoResult.Error);

        var cises = GisMtCisExtractor.Extract(infoResult.Value);
        var marksSaved = await _cisInfoSaver.SaveBatches(
            organisation,
            token,
            productGroup,
            cises,
            item.Number,
            cancellationToken);

        if (marksSaved.IsFailure)
            return Result.Failure<int>(marksSaved.Error);

        var documentEntity = new GisMtDocumentEntity
        {
            Id = item.Number,
            Number = item.Number,
            DocDate = item.DocDate ?? DateTime.UtcNow,
            Type = item.Type,
            Status = item.Status,
            SenderInn = item.SenderInn ?? string.Empty,
            ReceiverInn = item.ReceiverInn ?? organisation.INN,
            ProductGroup = string.IsNullOrWhiteSpace(item.ProductGroup) ? productGroup : item.ProductGroup,
            OrganisationInn = organisation.INN,
            MarksCount = marksSaved.Value,
            LoadedAt = DateTime.UtcNow
        };

        if (!await _documentRepository.Save(documentEntity))
            return Result.Failure<int>("Ошибка сохранения документа в CouchDB");

        return Result.Success(marksSaved.Value);
    }

    private async Task<int> CleanupMarks(string organisationInn, int retentionDays, CancellationToken cancellationToken)
    {
        if (retentionDays <= 0)
            retentionDays = 365;

        var olderThan = DateTime.UtcNow.AddDays(-retentionDays);
        var candidates = await _markRepository.GetExpiredForCleanup(olderThan, CleanupBatchSize);
        var deleted = 0;

        foreach (var mark in candidates)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (mark.OrganisationInn != organisationInn)
                continue;

            if (!(mark.Sold || mark.IsExpired))
                continue;

            if (await _markRepository.Delete(mark.Id))
                deleted++;
        }

        if (deleted > 0)
            _logger.LogInformation("Удалено {Count} устаревших марок остатка для {Inn}", deleted, organisationInn);

        return deleted;
    }
}
