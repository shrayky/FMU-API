using System.Net.Http.Headers;
using FmuApiDomain.TsPiot.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using TsPiotClinet.Services;

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
        });;

        services.AddScoped<ITsPiotService, TsPiotService>();
    }   
}

// тестовый пиот (эмулятор)
// https://tspiot.sandbox.crptech.ru/?mode=online&tab=marks
// MDEwNDYwNjIyNDQ5NzA1MTIxNWdoY1JhPSYuUWl0SB05MUVFMTEdOTJXZ0RWQ0FHRHJ0WDU0Y3AxdjdSK0VBcUZBWTZkRi90SW5PdVlKMU15UG1VPQ==