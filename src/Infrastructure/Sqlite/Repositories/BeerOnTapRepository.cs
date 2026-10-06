using CouchDb.DatabaseScheme;
using CSharpFunctionalExtensions;
using FmuApiDomain.BeerTaps.Entities;
using FmuApiDomain.BeerTaps.Interfaces;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.State.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sqlite.Documents;
using System.Text.Json;

namespace Sqlite.Repositories;

/// <summary>
/// Пивные краны в файле SQLite.
/// </summary>
public class BeerOnTapRepository(
    ILogger<BeerOnTapRepository> logger,
    SqliteDbContext context,
    IParametersService appConfiguration,
    IApplicationState applicationState)
    : BaseSqliteRepository<BeerTapEntity, BeerOnTapRow>(logger, context, context.BeerOnTap, appConfiguration, applicationState), IBeerOnTapRepository
{
    public override string DatabaseName => DatabaseNames.BeerOnTaps;

    public async Task<Result> FreeTap(string id)
    {
        var document = await GetById(id);

        // на кранах нет этой марки
        if (document == null)
            return Result.Success();

        var rowDeleted = await Delete(id);

        return rowDeleted ? Result.Success() : Result.Failure("Снятие с крана прошло с ошибкой.");
    }

    public async Task<Result> SetOnTap(string id, string mark, string wareName, string wareCode, int volune, string tapName = "")
    {
        var entity = new BeerTapEntity()
        {
            Id = id,
            MarkCode = mark,
            WareName = wareName,
            WareCode = wareCode,
            Volume = volune,
            TapName = tapName,
            LastUpdate = new DateTimeOffset(DateTime.SpecifyKind(DateTime.Now.Date, DateTimeKind.Utc)).ToUnixTimeSeconds()
        };

        var document = await GetById(id);

        // марка уже стоит на кране — продажи сохраняем
        if (document != null)
            entity.Sales = document.Sales;

        var rowSaved = await Save(entity);

        return rowSaved ? Result.Success() : Result.Failure("Постановка на кран прошла с ошибкой.");
    }

    public async Task<Result<int>> BeerKegVolume(string id)
    {
        var document = await GetById(id);

        return document == null ? 0 : document.Volume;
    }

    public async Task<Result<List<BeerTapEntity>>> All()
    {
        var entities = await ExecuteSafetyDbOperation(
            async () => await _rows
                .OrderBy(row => row.Id)
                .Take(_configuration.QueryLimit)
                .ToListAsync(),
            "All",
            (List<BeerOnTapRow>?)null);

        if (entities == null)
            return Result.Failure<List<BeerTapEntity>>(DatabaseUnavailable);

        var answer = entities
            .Select(ToEntity)
            .Where(entity => entity != null)
            .Select(entity => entity!)
            .ToList();

        return Result.Success(answer);
    }

    public async Task<Result> AddSale(string sGtin, int saledVolume)
    {
        var document = await GetById(sGtin);

        if (document == null)
            return Result.Failure($"На кранах нет марки {sGtin}");

        document.Sales += saledVolume;

        var isSuccess = await Save(document);

        return isSuccess ? Result.Success() : Result.Failure($"Не удалось обновить данные по продажам по марке {sGtin}");
    }

    public async Task<Result> LinkMarkToTap(string sGtin, string tapName)
    {
        var document = await GetById(sGtin);

        if (document == null)
            return Result.Failure($"На кранах нет марки {sGtin}");

        document.TapName = tapName;

        var isSuccess = await Save(document);

        return isSuccess ? Result.Success() : Result.Failure($"Не удалось обновить данные по крану по марке {sGtin}");
    }

    protected override BeerOnTapRow CreateRow(BeerTapEntity entity, string id) => new()
    {
        Id = id,
        Json = JsonSerializer.Serialize(entity, JsonOptions)
    };
}
