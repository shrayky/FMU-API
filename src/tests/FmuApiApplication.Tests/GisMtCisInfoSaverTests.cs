using CSharpFunctionalExtensions;
using FmuApiApplication.GisMt;
using FmuApiDomain.Configuration.Options.Organization;
using FmuApiDomain.GisMt;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Models;
using FmuApiDomain.TrueApi.ProductInfo;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FmuApiApplication.Tests;

public class GisMtCisInfoSaverTests
{
    [Fact]
    public async Task Загрузка_остатков_не_стирает_документ_марки()
    {
        var repository = new FakeMarkRepository(Mark("УПД-1"));
        var saver = Saver(repository);

        var result = await saver.SaveBatches(Organisation(), "token", "milk", ["cis-1"], GisMtMarkSources.Stock);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value);
        Assert.Equal("УПД-1", Assert.Single(repository.Saved).SourceDocumentId);
    }

    [Fact]
    public async Task Документ_становится_источником_марки()
    {
        var repository = new FakeMarkRepository(Mark(GisMtMarkSources.Stock));
        var saver = Saver(repository);

        var result = await saver.SaveBatches(Organisation(), "token", "milk", ["cis-1"], "УПД-2");

        Assert.True(result.IsSuccess);
        Assert.Equal("УПД-2", Assert.Single(repository.Saved).SourceDocumentId);
    }

    [Fact]
    public async Task Отклонённые_коды_не_сохраняются()
    {
        var repository = new FakeMarkRepository();
        var saver = new GisMtCisInfoSaver(
            NullLogger<GisMtCisInfoSaver>.Instance,
            new FakeCisesClient(answer: false),
            repository);

        var result = await saver.SaveBatches(Organisation(), "token", "milk", ["cis-1"], "УПД-3");

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
        Assert.Empty(repository.Saved);
    }

    private static GisMtCisInfoSaver Saver(IGisMtMarkRepository repository) =>
        new(NullLogger<GisMtCisInfoSaver>.Instance, new FakeCisesClient(), repository);

    private static PrintGroupData Organisation() => new()
    {
        Id = 5,
        INN = "2464122182",
        Name = "Магазин"
    };

    private static GisMtMarkEntity Mark(string sourceDocumentId) => new()
    {
        Id = "sgtin-1",
        SGtin = "sgtin-1",
        Cis = "cis-1",
        Gtin = "04610050691842",
        ProductGroup = "milk",
        OrganisationInn = "2464122182",
        SourceDocumentId = sourceDocumentId,
        InfoLoadedAt = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private sealed class FakeCisesClient(bool answer = true) : IGisMtCisesClient
    {
        public Task<Result<List<CisInfoResponseItem>>> CisesInfo(
            string token,
            IReadOnlyList<string> cises,
            string? productGroup,
            CancellationToken cancellationToken = default)
        {
            var response = cises
                .Select(cis => answer
                    ? new CisInfoResponseItem
                    {
                        CisInfo = new CisInfoData
                        {
                            Cis = cis,
                            RequestedCis = cis,
                            PrintView = "sgtin-1",
                            Gtin = "04610050691842",
                            ProductGroup = "milk",
                            ProductGroupId = 2,
                            OwnerInn = "2464122182",
                            Status = "INTRODUCED"
                        }
                    }
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

        public List<GisMtMarkEntity> Saved { get; } = [];

        public Task<GisMtMarkEntity?> Get(string id) =>
            Task.FromResult(_marks.FirstOrDefault(mark => mark.Id == id));

        public Task<List<GisMtMarkEntity>> GetListById(List<string> ids) =>
            Task.FromResult(_marks.Where(mark => ids.Contains(mark.Id)).ToList());

        public Task<bool> Save(GisMtMarkEntity entity) => Task.FromResult(true);

        public Task<bool> SaveRange(IEnumerable<GisMtMarkEntity> entities)
        {
            Saved.AddRange(entities);
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
            Task.FromResult(new List<GisMtMarkEntity>());

        public Task<List<GisMtMarkEntity>> GetBySourceDocument(string sourceDocumentId, int limit) =>
            Task.FromResult(_marks.Where(mark => mark.SourceDocumentId == sourceDocumentId).Take(limit).ToList());

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
