using CSharpFunctionalExtensions;
using FmuApiApplication.GisMt;
using FmuApiDomain.Configuration.Options.Organization;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Models;
using FmuApiDomain.TrueApi.ProductInfo;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FmuApiApplication.Tests;

public class GisMtWithdrawnMarksServiceTests
{
    private static readonly DateTime LoadStartedAt = new(2026, 11, 9, 3, 0, 0, DateTimeKind.Utc);
    private static readonly PrintGroupData Organisation = new() { INN = "246412218294" };

    [Fact]
    public async Task Выбывшая_марка_помечается_проданной()
    {
        var mark = Mark("sgtin-1", "cis-1");
        var repository = new FakeMarkRepository(mark);
        var cises = new FakeCisesClient()
            .Respond("cis-1", CisInfo("cis-1", "sgtin-1", "RETIRED"));

        var service = Service(repository, cises);

        var withdrawn = await service.Reconcile(Organisation, "token", "milk", Fresh(), LoadStartedAt);

        Assert.Equal(1, withdrawn);
        Assert.True(mark.Sold);
        Assert.Equal("RETIRED", mark.Status);
        Assert.True(mark.InfoLoadedAt > LoadStartedAt);
        Assert.Single(cises.Requests);
    }

    [Fact]
    public async Task Марка_в_обороте_остаётся_непроданной_и_её_дата_обновляется()
    {
        var mark = Mark("sgtin-2", "cis-2");
        var repository = new FakeMarkRepository(mark);
        var cises = new FakeCisesClient()
            .Respond("cis-2", CisInfo("cis-2", "sgtin-2", "INTRODUCED"));

        var service = Service(repository, cises);

        var withdrawn = await service.Reconcile(Organisation, "token", "milk", Fresh(), LoadStartedAt);

        Assert.Equal(0, withdrawn);
        Assert.False(mark.Sold);
        Assert.Equal("INTRODUCED", mark.Status);
        Assert.True(mark.InfoLoadedAt > LoadStartedAt);
        Assert.Single(cises.Requests);
    }

    [Fact]
    public async Task Марка_из_свежего_остатка_не_перепроверяется()
    {
        var mark = Mark("sgtin-3", "cis-3");
        var repository = new FakeMarkRepository(mark);
        var cises = new FakeCisesClient();

        var service = Service(repository, cises);

        var withdrawn = await service.Reconcile(Organisation, "token", "milk", Fresh("cis-3"), LoadStartedAt);

        Assert.Equal(0, withdrawn);
        Assert.Empty(cises.Requests);
        Assert.True(mark.InfoLoadedAt > LoadStartedAt);
    }

    [Fact]
    public async Task Страница_свежего_остатка_не_задерживает_сверку_выбывшей_марки()
    {
        var fresh = Enumerable
            .Range(0, 1000)
            .Select(index => Mark($"sgtin-fresh-{index}", $"cis-fresh-{index}"))
            .ToList();

        foreach (var mark in fresh)
            mark.InfoLoadedAt = LoadStartedAt.AddHours(-2);

        var old = Mark("sgtin-old", "cis-old");
        old.InfoLoadedAt = LoadStartedAt.AddHours(-1);

        var repository = new FakeMarkRepository([.. fresh, old]);
        var cises = new FakeCisesClient()
            .Respond("cis-old", CisInfo("cis-old", "sgtin-old", "RETIRED"));

        var service = Service(repository, cises);

        var withdrawn = await service.Reconcile(
            Organisation,
            "token",
            "milk",
            Fresh([.. fresh.Select(mark => mark.Cis)]),
            LoadStartedAt);

        Assert.Equal(1, withdrawn);
        Assert.True(old.Sold);
        Assert.Single(cises.Requests);
    }

    [Fact]
    public async Task Ошибка_cises_info_прерывает_сверку_без_сохранения()
    {
        var mark = Mark("sgtin-4", "cis-4");
        var repository = new FakeMarkRepository(mark);
        var cises = new FakeCisesClient().Fail("TrueAPI недоступен");

        var service = Service(repository, cises);

        var withdrawn = await service.Reconcile(Organisation, "token", "milk", Fresh(), LoadStartedAt);

        Assert.Equal(0, withdrawn);
        Assert.False(mark.Sold);
        Assert.Equal(0, repository.SaveRangeCalls);
    }

    [Fact]
    public async Task Ошибка_cises_info_сохраняет_даты_свежих_марок()
    {
        var freshMark = Mark("sgtin-fresh", "cis-fresh");
        var oldMark = Mark("sgtin-old", "cis-old");
        var repository = new FakeMarkRepository(freshMark, oldMark);
        var cises = new FakeCisesClient().Fail("TrueAPI недоступен");

        var service = Service(repository, cises);

        var withdrawn = await service.Reconcile(
            Organisation,
            "token",
            "milk",
            Fresh("cis-fresh"),
            LoadStartedAt);

        Assert.Equal(0, withdrawn);
        Assert.False(oldMark.Sold);
        Assert.True(freshMark.InfoLoadedAt > LoadStartedAt);
        Assert.Equal(1, repository.SaveRangeCalls);
    }

    [Fact]
    public async Task Марка_без_сведений_в_ответе_не_меняет_статус()
    {
        var mark = Mark("sgtin-5", "cis-5");
        var repository = new FakeMarkRepository(mark);
        var cises = new FakeCisesClient();

        var service = Service(repository, cises);

        var withdrawn = await service.Reconcile(Organisation, "token", "milk", Fresh(), LoadStartedAt);

        Assert.Equal(0, withdrawn);
        Assert.False(mark.Sold);
        Assert.True(mark.InfoLoadedAt > LoadStartedAt);
    }

    [Fact]
    public async Task Марки_другой_организации_в_сверку_не_попадают()
    {
        var own = Mark("sgtin-6", "cis-6");
        var foreign = Mark("sgtin-7", "cis-7", "7700000000");
        var repository = new FakeMarkRepository(own, foreign);
        var cises = new FakeCisesClient()
            .Respond("cis-6", CisInfo("cis-6", "sgtin-6", "RETIRED"))
            .Respond("cis-7", CisInfo("cis-7", "sgtin-7", "RETIRED"));

        var service = Service(repository, cises);

        var withdrawn = await service.Reconcile(Organisation, "token", "milk", Fresh(), LoadStartedAt);

        Assert.Equal(1, withdrawn);
        Assert.True(own.Sold);
        Assert.False(foreign.Sold);
        Assert.Single(cises.Requests);
        Assert.Equal(["cis-6"], cises.Requests[0]);
    }

    private static GisMtWithdrawnMarksService Service(IGisMtMarkRepository repository, IGisMtCisesClient cises) =>
        new(NullLogger<GisMtWithdrawnMarksService>.Instance, cises, repository);

    private static GisMtMarkEntity Mark(string sgtin, string cis, string? inn = null) => new()
    {
        Id = sgtin,
        SGtin = sgtin,
        Cis = cis,
        Gtin = "04602441015436",
        Status = "INTRODUCED",
        Sold = false,
        ProductGroup = "milk",
        OrganisationInn = inn ?? Organisation.INN,
        InfoLoadedAt = LoadStartedAt.AddHours(-1)
    };

    private static HashSet<string> Fresh(params string[] codes) => new(codes, StringComparer.Ordinal);

    private static CisInfoData CisInfo(string cis, string printView, string status) => new()
    {
        Cis = cis,
        RequestedCis = cis,
        PrintView = printView,
        Status = status,
        Gtin = "04602441015436"
    };

    private sealed class FakeCisesClient : IGisMtCisesClient
    {
        private readonly Dictionary<string, CisInfoData> _answers = new(StringComparer.Ordinal);
        private string? _failure;

        public List<IReadOnlyList<string>> Requests { get; } = [];

        public FakeCisesClient Respond(string cis, CisInfoData info)
        {
            _answers[cis] = info;
            return this;
        }

        public FakeCisesClient Fail(string error)
        {
            _failure = error;
            return this;
        }

        public Task<Result<List<CisInfoResponseItem>>> CisesInfo(
            string token,
            IReadOnlyList<string> cises,
            string? productGroup,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(cises);

            if (_failure != null)
                return Task.FromResult(Result.Failure<List<CisInfoResponseItem>>(_failure));

            var response = cises
                .Select(cis => _answers.TryGetValue(cis, out var info)
                    ? new CisInfoResponseItem { CisInfo = info }
                    : new CisInfoResponseItem { ErrorCode = "404", ErrorMessage = "Не найдено" })
                .ToList();

            return Task.FromResult(Result.Success(response));
        }

        public Task<Result<CisSearchResponse>> SearchCises(
            string token,
            CisSearchRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Failure<CisSearchResponse>("не используется"));

        public Task<Result<ProductsInformationTrueApi>> ProductInfo(
            string token,
            List<string> gtins,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Failure<ProductsInformationTrueApi>("не используется"));
    }

    private sealed class FakeMarkRepository(params GisMtMarkEntity[] marks) : IGisMtMarkRepository
    {
        private readonly List<GisMtMarkEntity> _marks = [.. marks];

        public int SaveRangeCalls { get; private set; }

        public Task<GisMtMarkEntity?> Get(string id) =>
            Task.FromResult(_marks.FirstOrDefault(mark => mark.Id == id));

        public Task<List<GisMtMarkEntity>> GetListById(List<string> ids) =>
            Task.FromResult(_marks.Where(mark => ids.Contains(mark.Id)).ToList());

        public Task<bool> Save(GisMtMarkEntity entity) => Task.FromResult(true);

        public Task<bool> SaveRange(IEnumerable<GisMtMarkEntity> entities)
        {
            SaveRangeCalls++;
            return Task.FromResult(true);
        }

        public Task<Result<GisMtMarkEntity>> ChangeState(string sGtin, bool sold) =>
            Task.FromResult(Result.Failure<GisMtMarkEntity>("не используется"));

        public Task<List<GisMtMarkEntity>> GetExpiredForCleanup(DateTime olderThanUtc, int limit) =>
            Task.FromResult(new List<GisMtMarkEntity>());

        public Task<List<GisMtMarkEntity>> GetUnsoldLoadedBefore(
            string organisationInn,
            string productGroup,
            DateTime loadedBeforeUtc,
            int limit) =>
            Task.FromResult(_marks
                .Where(mark => !mark.Sold
                    && mark.OrganisationInn == organisationInn
                    && mark.ProductGroup == productGroup
                    && mark.InfoLoadedAt < loadedBeforeUtc)
                .OrderBy(mark => mark.InfoLoadedAt)
                .ThenBy(mark => mark.Id)
                .Take(limit)
                .ToList());

        public Task<List<GisMtMarkEntity>> GetBySourceDocument(string sourceDocumentId, int limit) =>
            Task.FromResult(_marks
                .Where(mark => mark.SourceDocumentId == sourceDocumentId)
                .Take(limit)
                .ToList());

        public Task<bool> Delete(string id) => Task.FromResult(true);

        public Task<Result<GisMtMarkSearchResult>> Search(
            string searchTerm,
            int page,
            int pageSize,
            string? productGroup = null,
            bool expiredOnly = false,
            bool? sold = null,
            string? organisationInn = null) =>
            Task.FromResult(Result.Failure<GisMtMarkSearchResult>("не используется"));

        public Task<List<string>> GetProductGroups(string organisationInn, int limit) =>
            Task.FromResult(new List<string>());
    }
}
