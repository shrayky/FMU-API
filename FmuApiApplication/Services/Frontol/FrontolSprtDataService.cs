using FmuApiDomain.Models.Frontol;
using FmuFrontolDb;
using Microsoft.EntityFrameworkCore;

namespace FmuApiApplication.Services.Frontol
{
    public class FrontolSprtDataService
    {
        private readonly string _connectionString = string.Empty;
        private readonly FrontolDbContext _db;

        public FrontolSprtDataService(string connectionString)
        {
            _connectionString = connectionString;
            _db = new FrontolDbContext(connectionString);
        }

        public FrontolSprtDataService(FrontolDbContext frontolDbContext)
        {
            _db = frontolDbContext;
        }

        public async Task<int> PrintGroupCodeByBarcodeAsync(string barCode)
        {
            var barcode = await _db.Barcodes.FirstOrDefaultAsync(b => b.WareBarcode == barCode);

            if (barcode == null)
                return 0;

            var sprt = await _db.Sprts.FirstOrDefaultAsync(s => s.Id == barcode.WareId);

            if (sprt == null)
                return 0;

            if (sprt.PrintGroupForCheck == 0)
                return 0;

            var pg = await _db.PrintGroups.FirstOrDefaultAsync(pg => pg.Id == sprt.PrintGroupForCheck);

            if (pg == null) 
                return 0;
            
            return pg.Code;
        }
    }
}
