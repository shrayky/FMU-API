using FmuApiApplication.Mark.Interfaces;
using FmuApiApplication.Mark.Services;
using FmuApiDomain.MarkInformation.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FmuApiApplication.Mark
{
    public static class MarkServiceRegistration
    {
        public static IServiceCollection AddMarkServices(this IServiceCollection services)
        {
            services.AddScoped<IMarkParser, MarkParser>();
            services.AddScoped<IMarkChecker, MarkChecker>();
            services.AddScoped<IMarkStateManager, MarkStateManager>();
            services.AddScoped<IMarkFabric, MarkFabric>();            

            return services;
        }
    }
}
