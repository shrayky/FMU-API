using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Frontol.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace FrontolDb.Repository;

public class FrontolSprTRepositoryFactory : IFrontolSprTServiceFactory
{
    private readonly IMemoryCache _cacheService;
    private readonly IParametersService _parametersService;

    public FrontolSprTRepositoryFactory(IMemoryCache cacheService, IParametersService parametersService)
    {
        _cacheService = cacheService;
        _parametersService = parametersService;
    }

    public IDisposableFrontolSprTService Create(string connectionString)
        => new FrontolSprTRepo(connectionString, _cacheService, _parametersService);
}
