using CSharpFunctionalExtensions;
using FmuApiDomain.Frontol.DTO;

namespace FmuApiDomain.Frontol.Interfaces;

public interface IBeerTapsRepository
{
    Task<Result> SetOnTap(BeerTap berTap);

    Task<Result> FreeTapByMark(string markCode);

    Task<Result<List<BeerTap>>> All();
}
