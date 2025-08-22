using FmuApiDomain.Configuration.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ApplicationConfigurationService
{
    public static class ApplicationConfiguration
    {
        public static void AddService(IServiceCollection services)
        {
            services.AddSingleton<IParametersService, SimpleParametersService>();
        }
    }
}