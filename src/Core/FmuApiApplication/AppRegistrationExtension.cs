using System.Reflection;
using FmuApiApplication.ViewData.ApplicationMonitoring.Workers;
using FmuApiDomain.Attributes;
using FrontolDb.Workers;
using Microsoft.Extensions.DependencyInjection;

namespace FmuApiApplication;

public class AppRegistrationExtension
{
    public static void AddAppServices(IServiceCollection services)
    {
        services.AddAutoRegisteredServices([Assembly.GetExecutingAssembly()]);

        services.AddHostedService<CalculateLongTimeStatisticsWorker>();

        services.AddHostedService<BeerTapsSynchronizationWorker>();
    }
}