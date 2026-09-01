using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Frontol.Interfaces;
using FmuApiDomain.Frontol.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FrontolDb.Repository;

public class FrontolSprTRepo : IFrontolSprTService, IDisposableFrontolSprTService
{
    private readonly string _connectionString = string.Empty;
    private readonly FrontolDbContext _db;
    private readonly IMemoryCache _cacheService;
    private readonly IParametersService _parametersService;
    private readonly bool _ownsContext;

    private readonly int _cacheExpirationMinutes = 240;

    public FrontolSprTRepo(string connectionString, IMemoryCache cacheService, IParametersService parametersService)
    {
        _connectionString = connectionString;
        _ownsContext = true;

        _db = new FrontolDbContext(connectionString);
        _db.Database.SetCommandTimeout(TimeSpan.FromSeconds(2));

        _cacheService = cacheService;
        _parametersService = parametersService;
    }

    public FrontolSprTRepo(FrontolDbContext frontolDbContext, IMemoryCache cacheService, IParametersService parametersService)
    {
        _db = frontolDbContext;
        _ownsContext = false;

        _cacheService = cacheService;
        _parametersService = parametersService;
    }

    public async Task<Result<int>> PrintGroupCodeByBarcodeAsync(string barCode)
    {
        var appParams = await _parametersService.CurrentAsync();

        var frontolConnetionId = appParams.ConnectedFrontolSettings.PrintGroupSourseId;

        if (frontolConnetionId == 0)
            return Result.Success(0);

        if (appParams.OrganisationConfig.PrintGroups.Count <= 1)
            return Result.Success(0);

        if (barCode.Length == 0)
            return Result.Success(0);

        var code = 0;

        try
        {
            code = await PrintGroupCodeByWareBarcodeAsync(barCode);
        }
        catch (Exception e)
        {
            return Result.Failure<int>(e.Message);
        }

        return Result.Success(code);
    }

    private async Task<int> PrintGroupCodeByWareBarcodeAsync(string barCode)
    {
        var printGroupCode = _cacheService.Get<int>(barCode);

        if (printGroupCode != 0)
            return printGroupCode;

        var barcode = await _db.Barcodes.FirstOrDefaultAsync(b => b.WareBarcode == barCode);

        if (barcode == null)
            return 0;

        var sprt = await _db.Sprts.FirstOrDefaultAsync(s => s.Id == barcode.WareId);

        if (sprt == null)
            return 0;

        if (sprt.FiscalPrinterGroupCode() == 0)
            return 0;

        var pg = await _db.PrintGroups.FirstOrDefaultAsync(pg => pg.Id == sprt.FiscalPrinterGroupCode());

        if (pg == null)
            return 0;

        _cacheService.Set(barCode, pg.Code, TimeSpan.FromMinutes(_cacheExpirationMinutes));

        return pg.Code;
    }

    public async Task<Result<Dictionary<int, FrontolWare>>> GetWaresByIdsAsync(IReadOnlyCollection<int> wareIds)
    {
        if (wareIds.Count == 0)
            return Result.Success(new Dictionary<int, FrontolWare>());

        try
        {
            var uniqueIds = wareIds.Distinct().ToList();

            var sprts = await _db.Sprts
                .AsNoTracking()
                .Where(s => uniqueIds.Contains(s.Id))
                .ToListAsync();

            var wares = sprts.ToDictionary(
                s => s.Id,
                s => new FrontolWare
                {
                    Id = s.Id,
                    Code = s.Code,
                    Name = s.Name ?? string.Empty
                });

            return Result.Success(wares);
        }
        catch (Exception e)
        {
            return Result.Failure<Dictionary<int, FrontolWare>>(e.Message);
        }
    }

    public void Dispose()
    {
        if (_ownsContext)
            _db.Dispose();
    }
}
