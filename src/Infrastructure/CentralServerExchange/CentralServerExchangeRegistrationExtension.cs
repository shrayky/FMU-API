using System.Net.Http.Headers;
using System.Reflection;
using CentralServerExchange.Services;
using CentralServerExchange.Workers;
using FmuApiDomain.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace CentralServerExchange;

public static class CentralServerExchangeRegistrationExtension
{
    public static IServiceCollection AddExchangeWithFmuApiCentral(this IServiceCollection services)
    {
        services.AddAutoRegisteredServices([Assembly.GetExecutingAssembly()]);
        
        services.AddHostedService<CentralServerExchangeWorker>();

        services.AddHttpClient<CentralServerExchangeService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        });
        
        return services;
    }
}