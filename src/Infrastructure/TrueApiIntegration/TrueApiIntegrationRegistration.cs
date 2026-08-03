using FmuApiDomain.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Reflection;
using TrueApiIntegration.Services;
using TrueApiIntegration.Workers;

namespace TrueApiIntegration;

public static class TrueApiIntegrationRegistration
{
    public static void AddService(IServiceCollection services)
    {
        services.AddAutoRegisteredServices([Assembly.GetExecutingAssembly()]);

        services.AddHttpClient("TrueApiIntegration", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

        });

        services.AddHttpClient(GisMtTrueApiHttp.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

        });

        services.AddHostedService<TrueApiTokenLoaderWorker>();
        services.AddHostedService<GisMtDocumentsSyncWorker>();
        services.AddHostedService<GisMtStockLoadWorker>();
    }

}
