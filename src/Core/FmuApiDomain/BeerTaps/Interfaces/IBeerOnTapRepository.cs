using CSharpFunctionalExtensions;
using FmuApiDomain.BeerTaps.Entities;

namespace FmuApiDomain.BeerTaps.Interfaces;

public interface IBeerOnTapRepository
{
    Task<Result> SetOnTap(string id, string mark, string wareName, string awareCode, int volune);

    Task<Result> FreeTap(string id);

    Task<Result<int>> BeerKegVolume(string id);

    Task<Result<List<BeerTapEntity>>> All();

    Task<Result> AddSale(string sGtin, int saledVolume);

    Task<Result> LinkMarkToTap(string sgtin, string tapName);
}
