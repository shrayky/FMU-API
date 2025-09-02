using AutoUpdateWorkerService.Workers;
using Microsoft.Extensions.DependencyInjection;

namespace AutoUpdateWorkerService;

public static class AutoUpdateRegistrationExtension
{
    public static void AddService(IServiceCollection services)
    {
        services.AddHostedService<AutoUpdateWorker>();
    }
}