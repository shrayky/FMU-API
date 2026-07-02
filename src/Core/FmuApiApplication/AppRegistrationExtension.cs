using FmuApiApplication.BeerTaps.Workers;
using FmuApiApplication.ViewData.ApplicationMonitoring.Workers;
using FmuApiDomain.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

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