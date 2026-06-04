using FmuApiDomain.Frontol.Interfaces;
using FrontolDb.Repository;
using FrontolDb.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FrontolDb;

public class FrontolDbService
{
    public static void AddService(IServiceCollection services)
    {
        services.AddDbContext<FrontolDbContext>(options => { });

        services.AddScoped<IFrontolSprTService, FrontolSprTRepo>();

        // для подключений баз фронтола
        services.AddScoped<IBeerTapsRepositoryFactory, BeerTapsRepositoryFactory>();
        // для подключения к основной базе через di
        services.AddScoped<IBeerTapsRepository, BeerTapsRepo>();

        services.AddSingleton<FrontolAdminIniReader>();
    }
}
