using FmuApiDomain.Cache.Interfaces;
using FmuApiDomain.Configuration.Interfaces;
using MemoryCache;
using Microsoft.Extensions.DependencyInjection;

namespace ApplicationConfigurationService
{
    public static class ApplicationConfiguration
    {
        public static void AddService(IServiceCollection services)
        {
            services.AddSingleton<ICacheService, MemoryCacheService>();
            services.AddSingleton<IParametersService, SimpleParametersService>();
        }
    }
}