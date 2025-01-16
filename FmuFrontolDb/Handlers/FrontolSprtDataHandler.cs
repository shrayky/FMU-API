﻿using CSharpFunctionalExtensions;
using FrontolDb;
using Microsoft.EntityFrameworkCore;

namespace FrontolDb.Handlers
{
    public class FrontolSprtDataHandler
    {
        private readonly string _connectionString = string.Empty;
        private readonly FrontolDbContext _db;

        public FrontolSprtDataHandler(string connectionString)
        {
            _connectionString = connectionString;
            _db = new FrontolDbContext(connectionString);
        }

        public FrontolSprtDataHandler(FrontolDbContext frontolDbContext)
        {
            _db = frontolDbContext;
        }

        public async Task<Result<int>> PrintGroupCodeByBarcodeAsync(string barCode)
        {
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

            return pg.Code;
        }
    }
}
