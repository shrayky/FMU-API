using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Frontol.DTO;
using FmuApiDomain.Frontol.Interfaces;
using FrontolDb.Models;
using Microsoft.EntityFrameworkCore;

namespace FrontolDb.Repository;

public class BeerTapsRepo : IBeerTapsRepository, IDisposableBeerTapsRepository
{
    private readonly string _connectionString = string.Empty;
    private readonly IParametersService _parametersService;

    private readonly FrontolDbContext _db;
    private readonly bool _ownsContext;

    public BeerTapsRepo(string connectionString, IParametersService parametersService)
    {
        _connectionString = connectionString;
        _parametersService = parametersService;

        _ownsContext = true;

        _db = new FrontolDbContext(connectionString);
        _db.Database.SetCommandTimeout(TimeSpan.FromSeconds(2));
    }

    public BeerTapsRepo(FrontolDbContext frontolDbContext, IParametersService parametersService)
    {
        _db = frontolDbContext;
        _ownsContext = false;

        _parametersService = parametersService;
    }

    public async Task<Result<List<BeerTap>>> All()
    {
        try
        {
            var beertaps = await _db.BeerTaps.AsNoTracking().ToListAsync();

            List<BeerTap> data = [];

            foreach (var beerTap in beertaps)
            {
                var beerTapDto = new BeerTap()
                {
                    MarkCode = beerTap.MarkCode ?? string.Empty,
                    TapCode = beerTap.TapCode,
                    TapName = beerTap.Name,
                    Volume = beerTap.Volume ?? 0,
                    WareArticle = beerTap.WareArticle ?? string.Empty,
                    WareCode = beerTap.WareCode ?? 0,
                    WareId = beerTap.WareId ?? 0
                };

                data.Add(beerTapDto);
            }

            return Result.Success(data);
        }
        catch (Exception ex)
        {
            return Result.Failure<List<BeerTap>>(ex.Message);
        }
    }

    public async Task<Result> SetOnTap(BeerTap beerTap)
    {
        try
        {
            var wareRow = await _db.Sprts.FirstOrDefaultAsync(p => p.Code == beerTap.WareCode);

            if (wareRow == null)
                return Result.Failure($"Товар с кодом {beerTap.WareCode} не найден");

            var beerTapsCount = await _db.BeerTaps.CountAsync();
            beerTapsCount++;

            var tapRow = await _db.BeerTaps.FirstOrDefaultAsync(p => p.Name == beerTap.TapName);
                        
            if (tapRow == null)
            {
                tapRow = new BeerTapEntity()
                {
                    Name = beerTap.TapName,
                    MarkCode = beerTap.MarkCode.Replace(@"\u001d", ((char)29).ToString()),
                    WareId = wareRow.Id,
                    Volume = beerTap.Volume,
                    WareCode = beerTap.WareCode,
                    WareArticle = wareRow.Mark,
                    TapCode = beerTapsCount,
                    Id = await GEN_ID(),
                };
                
                await _db.BeerTaps.AddAsync(tapRow);
            }
            else
            {
                tapRow.MarkCode = beerTap.MarkCode;
                tapRow.WareId = wareRow.Id;
                tapRow.Volume = beerTap.Volume;
                tapRow.WareCode = beerTap.WareCode;
                tapRow.WareArticle = wareRow.Mark;

                _db.BeerTaps.Update(tapRow);
            }

            await _db.SaveChangesAsync();

            return Result.Success();

        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> FreeTapByMark(string markCode)
    {
        try
        {
            var exist = await _db.BeerTaps.FirstOrDefaultAsync(p => p.MarkCode == markCode);

            if (exist == null)
                return Result.Success();

            exist.WareId = null;
            exist.MarkCode = null;
            exist.Volume = null;
            exist.WareCode = null;
            exist.WareArticle = null;

            _db.Update(exist);

            await _db.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    private async Task<int> GEN_ID()
    {
        var connection = _db.Database.GetDbConnection();
        var wasOpen = connection.State == System.Data.ConnectionState.Open;

        if (!wasOpen)
            await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT GEN_ID(GCHNG, 1) FROM RDB$DATABASE";
            command.CommandTimeout = 1;

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result ?? 0);
        }
        finally
        {
            if (!wasOpen)
                await connection.CloseAsync();
        }
    }

    public void Dispose()
    {
        if (_ownsContext)
            _db.Dispose();
    }
}