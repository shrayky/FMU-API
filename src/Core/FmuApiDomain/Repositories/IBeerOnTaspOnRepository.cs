using CSharpFunctionalExtensions;
using FmuApiDomain.Database.Dto;

namespace FmuApiDomain.Repositories;

public interface IBeerOnTapsRepository
{
    Task<Result> SetOnTap(string id, string mark, string wareName, string awareCode, int volune);

    Task<Result> FreeTap(string id);

    Task<Result<int>> BeerKegVolume(string id);

    Task<Result<List<BeerTapEntity>>> All();

    Task<Result> AddSale(string sGtin, int saledVolume);

    Task<Result> LinkMarkToTap(string sgtin, string tapName);
}
