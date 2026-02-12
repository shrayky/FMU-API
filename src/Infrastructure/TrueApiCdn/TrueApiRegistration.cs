using System.Reflection;
using FmuApiDomain.Attributes;
using Microsoft.Extensions.DependencyInjection;
using TrueApi.Workers;

namespace TrueApi;

public static class TrueApiRegistration
{
    public static void AddService(IServiceCollection services)
    {
        services.AddAutoRegisteredServices([Assembly.GetExecutingAssembly()]);

        services.AddHostedService<CdnLoaderWorker>();
    }
}