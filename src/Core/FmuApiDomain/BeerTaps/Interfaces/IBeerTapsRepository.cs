using CSharpFunctionalExtensions;
using FmuApiDomain.BeerTaps.Models;

namespace FmuApiDomain.BeerTaps.Interfaces;

public interface IBeerTapsRepository
{
    Task<Result> SetOnTap(BeerTap berTap);

    Task<Result> FreeTapByMark(string markCode);

    Task<Result<List<BeerTap>>> All();
}
