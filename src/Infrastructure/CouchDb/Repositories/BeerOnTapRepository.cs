using CouchDb.Documents;
using CouchDB.Driver.Extensions;
using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Database.Dto;
using FmuApiDomain.Repositories;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Logging;

namespace CouchDb.Repositories;

public class BeerOnTapRepository : BaseCouchDbRepository<BeerTapEntity>, IBeerOnTapsRepository
{
    public BeerOnTapRepository(ILogger<BeerOnTapRepository> logger, CouchDbContext context, IParametersService appConfiguration, IApplicationState applicationState) : base(logger, context, context.BeerOnTap, appConfiguration, applicationState)
    {

    }

    public async Task<Result> FreeTap(string id)
    {
        if (_context == null)
            return new();

        var document = await GetByIdAsync(id);

        // на кранах нет этой марки
        if (document == null)
            return Result.Success();

        var rowDeleted = await DeleteAsync(id);

        return rowDeleted ? Result.Success() : Result.Failure("Снятие с крана прошло с ошибкой.");
    }

    public async Task<Result> SetOnTap(string id, string mark, string wareName, string wareCode, int volune)
    {
        if (_context == null)
            return new();

        var entity = new BeerTapEntity()
        {
            Id = id,
            MarkCode = mark,
            WareName = wareName,
            WareCode = wareCode,
            Volume = volune,
            LastUpdate = new DateTimeOffset(DateTime.SpecifyKind(DateTime.Now.Date, DateTimeKind.Utc)).ToUnixTimeSeconds()
        };

        var document = await GetByIdAsync(id);

        var rowSaved = false;

        // на кранах нет этой марки
        if (document == null)
            rowSaved = await CreateAsync(entity);
        else
            rowSaved = await UpdateAsync(id, entity);

        return rowSaved ? Result.Success() : Result.Failure("Постановка на кран прошла с ошибкой.");
    }

    public async Task<Result<int>> BeerKegVolume(string id)
    {
        if (_context == null)
            return new();

        var document = await GetByIdAsync(id);

        return document == null ? 0 : document.Volume;
    }

    public async Task<Result<List<BeerTapEntity>>> All()
    {
        if (_context == null)
            return Result.Failure<List<BeerTapEntity>>(DatabaseUnavailable);

        var queryLimit = _configuration.QueryLimit;

        var entities = await ExecuteSafetyDbOperation(
            async () => await _database.Take(queryLimit).ToListAsync(),
            "All",
            (List<CouchDoc<BeerTapEntity>>?)null);

        if (entities == null)
            return Result.Failure<List<BeerTapEntity>>(DatabaseUnavailable);

        List<BeerTapEntity> answer = entities.Select(node => node.Data).ToList();

        return Result.Success(answer);
    }

    public async Task<Result> AddSale(string sGtin, int saledVolume)
    {
        if (_context == null)
            return Result.Failure<List<BeerTapEntity>>(DatabaseUnavailable);

        var document = await GetByIdAsync(sGtin);

        if (document == null)
            return Result.Failure($"На кранах нет марки {sGtin}");

        document.Sales += saledVolume;

        var isSuccess = await UpdateAsync(sGtin, document);

        return isSuccess ? Result.Success() : Result.Failure($"Не удалось обновить данные по продажам по марке {sGtin}");
    }

    public async Task<Result> LinkMarkToTap(string sGtin, string tapName)
    {
        if (_context == null)
            return Result.Failure<List<BeerTapEntity>>(DatabaseUnavailable);

        var document = await GetByIdAsync(sGtin);

        if (document == null)
            return Result.Failure($"На кранах нет марки {sGtin}");

        document.TapName = tapName;

        var isSuccess = await UpdateAsync(sGtin, document);

        return isSuccess ? Result.Success() : Result.Failure($"Не удалось обновить данные по крану по марке {sGtin}");
    }
}
