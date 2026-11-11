using CSharpFunctionalExtensions;
using FmuApiDomain.BeerTaps.Models;

namespace FmuApiDomain.GisMt.Interfaces;

/// <summary>
/// Создание акта постановки на кран из операции Фронтола.
/// </summary>
public interface IGisMtConnectTapService
{
    Task<Result> CreateFromTap(TapBeerOperation operation, CancellationToken cancellationToken = default);
}
