using CSharpFunctionalExtensions;
using FmuApiDomain.Cache.Interfaces;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Frontol.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FrontolDb.Handlers
{
    public class FrontolSprTService : IFrontolSprTService
    {
        private readonly string _connectionString = string.Empty;
        private readonly FrontolDbContext _db;
        private readonly ICacheService _cacheService;
        private readonly IParametersService _parametersService;

        private readonly int _cacheExpirationMinutes = 240;

        public FrontolSprTService(string connectionString, ICacheService cacheService, IParametersService parametersService)
        {
            _connectionString = connectionString;
            
            _db = new FrontolDbContext(connectionString);

            _cacheService = cacheService;
            _parametersService = parametersService;
        }

        public FrontolSprTService(FrontolDbContext frontolDbContext, ICacheService cacheService, IParametersService parametersService)
        {
            _db = frontolDbContext;

            _cacheService = cacheService;
            _parametersService = parametersService;
        }

        public async Task<Result<int>> PrintGroupCodeByBarcodeAsync(string barCode)
        {
            var appParams = await _parametersService.CurrentAsync();

            if (!appParams.FrontolConnectionSettings.ConnectionEnable())
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
    }
}
