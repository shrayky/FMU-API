using System.Net;
using System.Security.Authentication;
using FmuApiApplication.InternetConnectionCheck.Workers;
using Microsoft.Extensions.DependencyInjection;

namespace FmuApiApplication.InternetConnectionCheck;

/// <summary>
/// Регистрация проверки доступности интернета.
/// </summary>
public static class InternetConnectionCheckRegistration
{
    public const string HttpClientName = "internetCheck";

    public static IServiceCollection AddInternetConnectionCheck(this IServiceCollection services)
    {
        services.AddHttpClient(HttpClientName, client =>
            {
                client.DefaultRequestVersion = HttpVersion.Version11;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false,
                SslProtocols = SslProtocols.Tls12,
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

        services.AddHostedService<InternetConnectionCheckWorker>();

        return services;
    }
}
