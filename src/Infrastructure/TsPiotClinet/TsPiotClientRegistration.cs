using FmuApiDomain.TsPiot.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using TsPiotClinet.Services;
using TsPiotClinet.Workers;

namespace TsPiotClinet;

public static class TsPiotClientRegistration
{
    public static void AddService(IServiceCollection services)
    {
        services.AddHttpClient("TsPiot", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        });

        services.AddHttpClient("TsPiotStateChecker", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(1);
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        });

        services.AddHttpClient("TsPiotVerisonChecker", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(1);
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        });

        services.AddKeyedScoped<ITsPiotService, TsPiotServiceV1>(1);
        services.AddKeyedScoped<ITsPiotService, TsPiotServiceV2>(2);
        services.AddKeyedScoped<ITsPiotService, TsPiotServiceV3>(3);

        services.AddScoped<ITsPiotService, TsPiotFabricService>();

        services.AddSingleton<TsPiotEspApiService>();

        services.AddHostedService<TsPiotStateCheckerWorker>();
    }
}

// тестовый пиот (эмулятор)
// https://tspiot.sandbox.crptech.ru/?mode=online&tab=marks
// MDEwNDYwNjIyNDQ5NzA1MTIxNWdoY1JhPSYuUWl0SB05MUVFMTEdOTJXZ0RWQ0FHRHJ0WDU0Y3AxdjdSK0VBcUZBWTZkRi90SW5PdVlKMU15UG1VPQ==