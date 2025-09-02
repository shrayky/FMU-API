using Microsoft.Extensions.DependencyInjection;
using ServicesAndDaemonsManager.Workers;

namespace ServicesAndDaemonsManager
{
    public static class ServicesAndDaemonsRegistrationExtension
    {
        public static void AddService(IServiceCollection services)
        {
            services.AddHostedService<ApplicationRestartWorker>();
        }
    }
}
