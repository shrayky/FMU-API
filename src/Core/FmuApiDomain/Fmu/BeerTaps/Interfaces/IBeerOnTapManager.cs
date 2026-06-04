using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.DTO.BeerTaps;
using FmuApiDomain.Fmu.BeerTaps.Models;

namespace FmuApiDomain.Fmu.BeerTaps.Interfaces;

public interface IBeerOnTapManager
{
    Task<Result> TapOperation(TapBeerOperation operation);
    Task<int> Volume(string sGtin);
    Task<List<BeerOnTap>> List();
    Task<Result> AddSale(string sGtin, int saledVolume);
    Task<Result> SyncFrontolBeerTaps(List<FrontolConnectionSettings> frontolConnections);
}
