using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Frontol.Interfaces;

namespace FrontolDb.Repository;

public class BeerTapsRepositoryFactory : IBeerTapsRepositoryFactory
{
    private readonly IParametersService _parametersService;
    public BeerTapsRepositoryFactory(IParametersService parametersService)
    {
        _parametersService = parametersService;
    }
    public IDisposableBeerTapsRepository Create(string connectionString)
        => new BeerTapsRepo(connectionString, _parametersService);

}
