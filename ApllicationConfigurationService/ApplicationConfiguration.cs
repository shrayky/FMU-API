using Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ApllicationConfigurationService
{
    public static class ApplicationConfiguration
    {
        public static void AddService(IServiceCollection services)
        {
            services.AddSingleton<IParametersService, SimpleParametersService>();
            services.AddSingleton<ICdnService, SimpleCdnService>();
        }
    }
}
