using System.Net.Http.Headers;
using FmuApiDomain.TsPiot.Interfaces;
using Microsoft.Extensions.DependencyInjection;

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
        });

        services.AddScoped<ITsPiotService, ITsPiotService>();
    }   
}

// тестовый пиот (эмулятор)
// https://tspiot.sandbox.crptech.ru/?mode=online&tab=marks