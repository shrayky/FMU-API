using FmuApiDomain.Cache;
using FmuApiDomain.Configuration;
using Interfaces;
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
            services.AddSingleton<ICdnService, SimpleCdnService>();
        }
    }
}
