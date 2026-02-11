using LocalModuleIntegration.Interfaces;
using LocalModuleIntegration.Service;
using LocalModuleIntegration.Workers;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;

namespace LocalModuleIntegration
{
    public static class LocalModuleRegistration
    {
        public static void AddService(IServiceCollection services, int localModuleVersion)
        {
            services.AddHttpClient("LocalModule", client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));
                })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));

            services.AddHttpClient("Enisey", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            });

            if (localModuleVersion == 2)
                services.AddSingleton<ILocalModuleService, LocalModuleServiceV2>();
            else
                services.AddSingleton<ILocalModuleService, LocalModuleServiceV1>();
            
            services.AddHostedService<LocalModuleStatusWorker>();
        }
    }
}
