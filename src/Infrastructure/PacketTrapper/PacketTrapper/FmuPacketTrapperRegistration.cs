using FmuApiDomain.Attributes;
using FmuPacketTrapper.Worker;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FmuPacketTrapper;

public static class FmuPacketTrapperRegistration
{
    public static void AddService(IServiceCollection services)
    {
        services.AddAutoRegisteredServices([Assembly.GetExecutingAssembly()]);

        services.AddHostedService<ClearOldMarkFilesWorker>();
    }
}
