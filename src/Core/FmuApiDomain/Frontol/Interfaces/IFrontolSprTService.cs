using CSharpFunctionalExtensions;
using FmuApiDomain.Frontol.Models;

namespace FmuApiDomain.Frontol.Interfaces;

public interface IFrontolSprTService
{
    Task<Result<int>> PrintGroupCodeByBarcodeAsync(string barCode);

    Task<Result<Dictionary<int, FrontolWare>>> GetWaresByIdsAsync(IReadOnlyCollection<int> wareIds);
}

public interface IFrontolSprTServiceFactory
{
    IDisposableFrontolSprTService Create(string connectionString);
}

public interface IDisposableFrontolSprTService : IFrontolSprTService, IDisposable
{
}