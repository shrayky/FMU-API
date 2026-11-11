using CSharpFunctionalExtensions;
using FmuApiApplication.GisMt.Journal;
using FmuApiApplication.Mark.Services;
using FmuApiApplication.State;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Configuration.Options.Organization;
using FmuApiDomain.Frontol.Interfaces;
using FmuApiDomain.Frontol.Models;
using FmuApiDomain.GisMt;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Journal;
using FmuApiDomain.GisMt.Models;
using FmuApiDomain.TrueApi.ProductInfo;
using Microsoft.Extensions.Logging.Abstractions;

namespace FmuApiApplication.Tests;

internal static class GisMtTestData
{
    public static Parameters Parameters(string inn = "2464122182", string kpp = "246401001", string fiasId = "")
    {
        var parameters = new Parameters
        {
            OrganisationConfig = new OrganizationConfiguration
            {
                PrintGroups =
                [
                    new PrintGroupData
                    {
                        Id = 5,
                        INN = inn,
                        Name = "Магазин",
                        Kpp = kpp,
                        FiasId = fiasId
                    }
                ]
            }
        };

        parameters.ConnectedFrontolSettings.FrontolWareDataSourceId = 1;
        parameters.ConnectedFrontolSettings.ConnectionSettings.Add(new FrontolConnectionSettings
        {
            Id = 1,
            Name = "Касса",
            Path = @"D:\frontol\base.fdb"
        });

        return parameters;
    }

    public static GisMtJournalEntity ConnectTapDocument(string uid = "uid-1") => new()
    {
        Id = Guid.NewGuid().ToString(),
        Kind = GisMtJournalKinds.ConnectTap,
        Status = GisMtJournalStatuses.Draft,
        Inn = "2464122182",
        Kpp = "246401001",
        Uid = uid,
        DocDate = new DateOnly(2026, 11, 2),
        CreatedAt = DateTime.Now,
        Lines = [new GisMtJournalLine { Cis = "0104603334003509215ABC", ConnectDate = new DateOnly(2026, 11, 2) }]
    };

    public static GisMtJournalService JournalService(
        IGisMtJournalRepository repository,
        IGisMtDocumentSigner signer,
        IGisMtLkDocumentsClient client,
        Parameters? parameters = null,
        IGisMtMarkRepository? marks = null,
        IGisMtDocumentsClient? documents = null,
        IGisMtCisHistoryClient? history = null)
    {
        return new GisMtJournalService(
            NullLogger<GisMtJournalService>.Instance,
            repository,
            new FakeParameters(parameters ?? Parameters()),
            TokenState(),
            signer,
            client,
            marks ?? new FakeMarksRepository(),
            documents ?? new FakeDocumentsClient(),
            history ?? new FakeCisHistoryClient());
    }

    /// <summary>
    /// Просроченная марка остатка ГИС МТ для подбора в списание по сроку годности.
    /// </summary>
    public static GisMtMarkEntity ExpiredMark(
        string cis,
        string productGroup,
        DateTime? expireDate,
        bool sold = false,
        string inn = "2464122182")
    {
        var gtin = GisMtCisGtin.Extract(cis);
        var sgtin = GisMtSgtinCalculator.Calculate(cis, gtin);

        return new GisMtMarkEntity
        {
            Id = sgtin,
            SGtin = sgtin,
            Cis = cis,
            Gtin = gtin,
            ProductGroup = productGroup,
            ExpireDate = expireDate,
            Sold = sold,
            OrganisationInn = inn,
            InfoLoadedAt = DateTime.UtcNow
        };
    }

    public static ApplicationState TokenState(string token = "token-1")
    {
        var state = new ApplicationState();
        state.UpdateTrueApiToken("2464122182", token, DateTime.Now.AddHours(1));

        return state;
    }

    public static MarkParser MarkParser() => new();

    public static GisMtConnectTapService ConnectTapService(
        IGisMtJournalRepository repository,
        bool documentsEnabled,
        bool autoCreate,
        bool autoSend,
        IGisMtLkDocumentsClient? client = null)
    {
        var parameters = Parameters();
        parameters.GisMtSettings = new GisMtSettings
        {
            DocumentsEnabled = documentsEnabled,
            AutoCreateConnectTap = autoCreate,
            AutoSendConnectTap = autoSend
        };

        var journalService = JournalService(repository, new FakeSigner(), client ?? new FakeLkClient(), parameters);

        return new GisMtConnectTapService(
            NullLogger<GisMtConnectTapService>.Instance,
            new FakeParameters(parameters),
            MarkParser(),
            journalService);
    }

    public static GisMtReceiptService ReceiptService(
        IGisMtJournalRepository repository,
        Dictionary<string, int> weights,
        decimal? price,
        IGisMtMarkRepository? marks = null,
        Dictionary<string, string>? packageTypes = null,
        Dictionary<string, List<string>>? aggregation = null,
        string? aggregationFailure = null)
    {
        var parameters = Parameters();
        var markRepository = marks ?? new FakeMarksRepository();

        return new GisMtReceiptService(
            NullLogger<GisMtReceiptService>.Instance,
            new FakeParameters(parameters),
            TokenState(),
            MarkParser(),
            new FakeCisesClient(weights, packageTypes),
            new FakeSprTFactory(price),
            JournalService(repository, new FakeSigner(), new FakeLkClient(), parameters, markRepository),
            markRepository,
            repository,
            new FakeAggregationClient(aggregation, aggregationFailure));
    }
}

internal sealed class FakeParameters(Parameters parameters) : IParametersService
{
    public Parameters Current() => parameters;

    public Task<Parameters> CurrentAsync() => Task.FromResult(parameters);

    public Task UpdateAsync(Parameters value) => Task.CompletedTask;

    public Task<Result> ApplyFromCentral(FmuApiDomain.CentralServiceExchange.Models.FmuApiSetting value) =>
        Task.FromResult(Result.Success());
}

internal sealed class FakeJournalRepository : IGisMtJournalRepository
{
    private readonly Dictionary<string, GisMtJournalEntity> _documents = [];

    /// <summary>
    /// Статус, записанный в «хранилище»: в памяти акт меняется до условной записи, как в настоящей базе.
    /// </summary>
    private readonly Dictionary<string, string> _statuses = [];

    public List<GisMtJournalEntity> Saved => _documents.Values.ToList();

    public int SaveCalls { get; private set; }

    /// <summary>
    /// Подмена прочитанного акта: позволяет проверить гонку чтения и отправки.
    /// </summary>
    public Func<GisMtJournalEntity?, GisMtJournalEntity?>? GetHook { get; set; }

    public Func<GisMtJournalEntity, bool>? SaveFilter { get; set; }

    /// <summary>
    /// Имитация проигранной гонки за статус отправки: другой вызов уже занял акт.
    /// </summary>
    public bool FailStatusClaim { get; set; }

    /// <summary>
    /// Сбой чтения уже отправленных КИ: подбор должен получить отказ, а не пустой набор.
    /// </summary>
    public string? SubmittedCisError { get; set; }

    public Task<GisMtJournalEntity?> Get(string id) =>
        Task.FromResult(GetHook is null ? _documents.GetValueOrDefault(id) : GetHook(_documents.GetValueOrDefault(id)));

    public Task<GisMtJournalEntity?> FindByUid(string uid) => Task.FromResult(
        _documents.Values.FirstOrDefault(document => document.Uid == uid));

    public Task<bool> Save(GisMtJournalEntity entity)
    {
        SaveCalls++;

        if (SaveFilter is not null && !SaveFilter(entity))
            return Task.FromResult(false);

        _documents[entity.Id] = entity;
        _statuses[entity.Id] = entity.Status;
        return Task.FromResult(true);
    }

    public Task<bool> TryUpdateStatus(GisMtJournalEntity entity, string expectedStatus)
    {
        if (FailStatusClaim)
            return Task.FromResult(false);

        if (!_statuses.TryGetValue(entity.Id, out var storedStatus)
            || !string.Equals(storedStatus, expectedStatus, StringComparison.Ordinal))
            return Task.FromResult(false);

        _documents[entity.Id] = entity;
        _statuses[entity.Id] = entity.Status;

        return Task.FromResult(true);
    }

    public Task<bool> Delete(string id)
    {
        _statuses.Remove(id);
        return Task.FromResult(_documents.Remove(id));
    }

    public Task<Result<GisMtJournalPage>> List(string kind, string status, int page, int pageSize) =>
        Task.FromResult(Result.Success(new GisMtJournalPage
        {
            Documents = _documents.Values.Take(pageSize).ToList(),
            Count = _documents.Count,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = 1
        }));

    public Task<List<GisMtJournalEntity>> GetPendingCrptStatus(DateTime checkedBefore, DateTime sentAfter, int limit) =>
        Task.FromResult(_documents.Values
            .Where(document => GisMtCrptStatuses.NeedsPolling(document, checkedBefore, sentAfter))
            .Take(limit)
            .ToList());

    public Task<Result<HashSet<string>>> GetSubmittedCis(string organisationInn, string productGroup, DateTime sentAfter, int limit)
    {
        if (!string.IsNullOrWhiteSpace(SubmittedCisError))
            return Task.FromResult(Result.Failure<HashSet<string>>(SubmittedCisError));

        var cises = _documents.Values
            .Where(document => document.Status == GisMtJournalStatuses.Sent
                && !GisMtCrptStatuses.IsFailed(document.CrptStatus)
                && string.Equals(document.Inn, organisationInn, StringComparison.OrdinalIgnoreCase)
                && string.Equals(document.ProductGroup, productGroup, StringComparison.OrdinalIgnoreCase)
                && (document.SentAt ?? document.CreatedAt) >= sentAfter)
            .Take(limit)
            .SelectMany(document => document.Lines)
            .Select(line => line.Cis)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Task.FromResult(Result.Success(cises));
    }
}

internal sealed class FakeMarksRepository(params GisMtMarkEntity[] marks) : IGisMtMarkRepository
{
    private readonly List<GisMtMarkEntity> _marks = marks.ToList();

    public IReadOnlyList<GisMtMarkEntity> Marks => _marks;

    public Task<GisMtMarkEntity?> Get(string id) =>
        Task.FromResult(_marks.FirstOrDefault(mark => mark.Id == id));

    public Task<List<GisMtMarkEntity>> GetListById(List<string> ids) =>
        Task.FromResult(_marks.Where(mark => ids.Contains(mark.Id)).ToList());

    public Task<bool> Save(GisMtMarkEntity entity)
    {
        _marks.Add(entity);
        return Task.FromResult(true);
    }

    public Task<bool> SaveRange(IEnumerable<GisMtMarkEntity> entities)
    {
        _marks.AddRange(entities);
        return Task.FromResult(true);
    }

    public Task<Result<GisMtMarkEntity>> ChangeState(string sGtin, bool sold)
    {
        var mark = _marks.FirstOrDefault(item => item.Id == sGtin || item.SGtin == sGtin);

        if (mark is null)
            return Task.FromResult(Result.Failure<GisMtMarkEntity>($"Марка {sGtin} не найдена"));

        mark.Sold = sold;

        return Task.FromResult(Result.Success(mark));
    }

    public Task<List<GisMtMarkEntity>> GetExpiredForCleanup(DateTime olderThanUtc, int limit) =>
        Task.FromResult(_marks.Where(mark => mark.IsExpired).Take(limit).ToList());

    public Task<List<GisMtMarkEntity>> GetUnsoldLoadedBefore(string organisationInn, string productGroup, DateTime loadedBeforeUtc, int limit) =>
        Task.FromResult(_marks
            .Where(mark => !mark.Sold && mark.OrganisationInn == organisationInn && mark.ProductGroup == productGroup)
            .Take(limit)
            .ToList());

    public Task<List<GisMtMarkEntity>> GetBySourceDocument(string sourceDocumentId, int limit) =>
        Task.FromResult(_marks.Where(mark => mark.SourceDocumentId == sourceDocumentId).Take(limit).ToList());

    public Task<bool> Delete(string id) => Task.FromResult(_marks.RemoveAll(mark => mark.Id == id) > 0);

    public Task<Result<GisMtMarkSearchResult>> Search(
        string searchTerm,
        int page,
        int pageSize,
        string? productGroup = null,
        bool expiredOnly = false,
        bool? sold = null,
        string? organisationInn = null)
    {
        var query = _marks.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(mark => mark.SGtin.Contains(searchTerm) || mark.Cis.Contains(searchTerm));

        if (!string.IsNullOrWhiteSpace(productGroup))
            query = query.Where(mark => mark.ProductGroup == productGroup);

        if (!string.IsNullOrWhiteSpace(organisationInn))
            query = query.Where(mark => mark.OrganisationInn == organisationInn);

        if (expiredOnly)
            query = query.Where(mark => mark.IsExpired);

        if (sold.HasValue)
            query = query.Where(mark => mark.Sold == sold.Value);

        var found = query.ToList();

        return Task.FromResult(Result.Success(new GisMtMarkSearchResult
        {
            Marks = found.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Count = found.Count,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = found.Count == 0 ? 1 : (int)Math.Ceiling(found.Count / (double)pageSize),
            SearchTerm = searchTerm
        }));
    }

    public Task<List<string>> GetProductGroups(string organisationInn, int limit) =>
        Task.FromResult(_marks
            .Where(mark => !mark.Sold && string.Equals(mark.OrganisationInn, organisationInn, StringComparison.OrdinalIgnoreCase))
            .Select(mark => mark.ProductGroup)
            .Where(group => !string.IsNullOrWhiteSpace(group))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList());
}

internal sealed class FakeAggregationClient(Dictionary<string, List<string>>? children = null, string? failure = null) : IGisMtCisAggregationClient
{
    public Task<Result<Dictionary<string, List<string>>>> AggregatedList(
        string token,
        IReadOnlyList<string> cises,
        string? productGroup,
        CancellationToken cancellationToken = default)
    {
        if (failure is not null)
            return Task.FromResult(Result.Failure<Dictionary<string, List<string>>>(failure));

        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var cis in cises)
        {
            if (children?.TryGetValue(cis, out var list) == true)
                result[cis] = list;
        }

        return Task.FromResult(Result.Success(result));
    }
}

internal sealed class FakeCisHistoryClient(GisMtCisHistoryRecord? record = null, string? failure = null) : IGisMtCisHistoryClient
{
    public int Calls { get; private set; }

    public Task<Result<List<GisMtCisHistoryRecord>>> History(
        string token,
        string cis,
        CancellationToken cancellationToken = default)
    {
        Calls++;

        if (failure is not null)
            return Task.FromResult(Result.Failure<List<GisMtCisHistoryRecord>>(failure));

        var value = record is null
            ? new List<GisMtCisHistoryRecord>()
            : new List<GisMtCisHistoryRecord> { record };

        return Task.FromResult(Result.Success(value));
    }
}

internal sealed class FakeDocumentsClient(GisMtDocInfoResponse? info = null, string? failure = null) : IGisMtDocumentsClient
{
    public int InfoCalls { get; private set; }

    public Task<Result<GisMtDocListResponse>> DocumentList(
        string token,
        string productGroup,
        string receiverInn,
        DateTime dateFrom,
        DateTime dateTo,
        IEnumerable<string> documentTypes,
        string? did,
        string? orderedColumnValue,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Result<GisMtDocInfoResponse>> DocumentInfo(
        string token,
        string documentId,
        string? productGroup,
        CancellationToken cancellationToken = default)
    {
        InfoCalls++;

        return Task.FromResult(failure is null
            ? Result.Success(info ?? new GisMtDocInfoResponse { Status = GisMtCrptStatuses.CheckedOk })
            : Result.Failure<GisMtDocInfoResponse>(failure));
    }
}

internal sealed class FakeSigner(string? failure = null) : IGisMtDocumentSigner
{
    public Result<string> SignDetached(string inn, string certificateNumber, string password, string payload) =>
        failure is null ? Result.Success("signature") : Result.Failure<string>(failure);
}

internal sealed class FakeLkClient(string? failure = null, Exception? exception = null) : IGisMtLkDocumentsClient
{
    public int Calls { get; private set; }

    public string? LastProductGroup { get; private set; }

    public Task<Result<string>> CreateDocument(
        string token,
        string productGroup,
        string type,
        string productDocument,
        string signature,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        LastProductGroup = productGroup;

        if (exception is not null)
            throw exception;

        return Task.FromResult(failure is null
            ? Result.Success("crpt-1")
            : Result.Failure<string>(failure));
    }
}

internal sealed class FakeCisesClient(Dictionary<string, int> weights, Dictionary<string, string>? packageTypes = null) : IGisMtCisesClient
{
    public Task<Result<List<CisInfoResponseItem>>> CisesInfo(
        string token,
        IReadOnlyList<string> cises,
        string? productGroup,
        CancellationToken cancellationToken = default)
    {
        var items = cises
            .Where(cis => weights.ContainsKey(cis))
            .Select(cis => new CisInfoResponseItem
            {
                CisInfo = new CisInfoData
                {
                    Cis = cis,
                    RequestedCis = cis,
                    ProductName = "Молоко пастеризованное 3,2%",
                    ProductWeightGr = weights[cis],
                    PackageType = packageTypes?.GetValueOrDefault(cis)
                }
            })
            .ToList();

        return Task.FromResult(Result.Success(items));
    }

    public Task<Result<CisSearchResponse>> SearchCises(string token, CisSearchRequest request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Result<ProductsInformationTrueApi>> ProductInfo(string token, List<string> gtins, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success(new ProductsInformationTrueApi()));
}

internal sealed class FakeSprTFactory(decimal? price) : IFrontolSprTServiceFactory
{
    public IDisposableFrontolSprTService Create(string connectionString) => new FakeSprTService(price);
}

internal sealed class FakeSprTService(decimal? price) : IDisposableFrontolSprTService
{
    public Task<Result<int>> PrintGroupCodeByBarcodeAsync(string barCode) =>
        Task.FromResult(Result.Success(0));

    public Task<Result<int>> WareTypeByBarcodeAsync(string barCode) =>
        Task.FromResult(Result.Success(0));

    public Task<Result<Dictionary<int, FrontolWare>>> GetWaresByIdsAsync(IReadOnlyCollection<int> wareIds) =>
        Task.FromResult(Result.Success(new Dictionary<int, FrontolWare>()));

    public Task<Result<decimal?>> RetailPriceByBarcode(string barcode) =>
        Task.FromResult(Result.Success(price));

    public void Dispose()
    {
    }
}
