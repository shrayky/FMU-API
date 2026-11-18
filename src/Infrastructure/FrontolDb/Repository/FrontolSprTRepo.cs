using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Frontol.Interfaces;
using FmuApiDomain.Frontol.Models;
using FrontolDb.Models;
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

        var frontolConnetionId = appParams.ConnectedFrontolSettings.ResolveWareDataSourceId();

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

    public async Task<Result<int>> WareTypeByBarcodeAsync(string barCode)
    {
        var appParams = await _parametersService.CurrentAsync();

        if (appParams.ConnectedFrontolSettings.ResolveWareDataSourceId() == 0)
            return Result.Success(0);

        if (barCode.Length == 0)
            return Result.Success(0);

        try
        {
            var wareType = await WareTypeByWareBarcodeAsync(barCode);
            return Result.Success(wareType);
        }
        catch (Exception e)
        {
            return Result.Failure<int>(e.Message);
        }
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

    private async Task<int> WareTypeByWareBarcodeAsync(string barCode)
    {
        var cacheKey = $"frontol-waretype:{barCode}";

        if (_cacheService.TryGetValue(cacheKey, out int cachedWareType))
            return cachedWareType;

        var barcode = await _db.Barcodes.FirstOrDefaultAsync(b => b.WareBarcode == barCode);

        if (barcode == null)
            return 0;

        var sprt = await _db.Sprts.FirstOrDefaultAsync(s => s.Id == barcode.WareId);

        if (sprt == null)
            return 0;

        _cacheService.Set(cacheKey, sprt.WareType, TimeSpan.FromMinutes(_cacheExpirationMinutes));

        return sprt.WareType;
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

    public async Task<Result<decimal?>> RetailPriceByBarcode(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return Result.Success<decimal?>(null);

        try
        {
            var code = await _db.Barcodes
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.WareBarcode == barcode && item.Deleted == 0);

            if (code == null)
                return Result.Success<decimal?>(null);

            var remain = await CurrentRemain(code);
            if (remain == null)
                return Result.Success<decimal?>(null);

            var price = await CurrentPrice(remain.Id);
            if (price is null or <= 0)
                return Result.Success<decimal?>(null);

            var factor = code.Factor == 0 ? 1d : code.Factor;
            var value = Math.Round((decimal)price.Value * (decimal)factor, 2, MidpointRounding.AwayFromZero);
            if (value <= 0)
                return Result.Success<decimal?>(null);

            return Result.Success<decimal?>(value);
        }
        catch (Exception e)
        {
            return Result.Failure<decimal?>(e.Message);
        }
    }

    /// <summary>
    /// Остаток с теми же разрезами, что у штрихкода: сначала предприятие 0 и пустой тип цены.
    /// </summary>
    private async Task<Remain?> CurrentRemain(Barcode code)
    {
        var remains = await _db.Remains
            .AsNoTracking()
            .Where(item => item.WareId == code.WareId
                && item.Deleted == 0
                && item.AspectValue1Id == code.AspectValue1Id
                && item.AspectValue2Id == code.AspectValue2Id
                && item.AspectValue3Id == code.AspectValue3Id
                && item.AspectValue4Id == code.AspectValue4Id
                && item.AspectValue5Id == code.AspectValue5Id)
            .ToListAsync();

        return remains
            .OrderBy(item => item.EnterpriseId == 0 ? 0 : 1)
            .ThenBy(item => item.PriceType == null ? 0 : 1)
            .ThenBy(item => item.EnterpriseId)
            .FirstOrDefault();
    }

    /// <summary>
    /// Цена остатка с наибольшей датой начала, которая уже наступила.
    /// </summary>
    private async Task<double?> CurrentPrice(int remainId)
    {
        // Frontol хранит ActDateTime без часового пояса.
        var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

        return await _db.PriceDatas
            .AsNoTracking()
            .Where(item => item.RemainId == remainId && item.ActDateTime <= now)
            .OrderByDescending(item => item.ActDateTime)
            .Select(item => (double?)item.Price)
            .FirstOrDefaultAsync();
    }

    public void Dispose()
    {
        if (_ownsContext)
            _db.Dispose();
    }
}
