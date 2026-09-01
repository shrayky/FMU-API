using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.BeerTaps.Models;

namespace FmuApiDomain.BeerTaps.Interfaces;

public interface IBeerOnTapManager
{
    Task<Result> TapOperation(TapBeerOperation operation);
    Task<int> Volume(string sGtin);
    Task<List<BeerOnTap>> List();
    Task<Result> AddSale(string sGtin, int saledVolume);
    Task<Result> SyncFrontolBeerTaps(List<FrontolConnectionSettings> frontolConnections);

    Task<Result<int>> LoadFromFrontol(int connectionId);
}
