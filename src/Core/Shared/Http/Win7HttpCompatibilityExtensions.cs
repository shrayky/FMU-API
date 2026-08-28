using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace Shared.Http;

/// <summary>
/// Регистрация совместимости HttpClient с Windows 7/8.
/// </summary>
public static class Win7HttpCompatibilityExtensions
{
    /// <summary>
    /// Для всех HttpClient на Windows 7/8 принудительно ставит HTTP/1.1 и TLS 1.2,
    /// не подменяя уже заданные PrimaryHandler (TsPiot, CouchDb, LocalModule).
    /// </summary>
    public static IServiceCollection AddWin7HttpCompatibility(this IServiceCollection services)
    {
        if (!ForceHttp11MessageHandler.IsRequiredOnThisOs)
            return services;

        services.PostConfigureAll<HttpClientFactoryOptions>(options =>
        {
            options.HttpClientActions.Add(client =>
            {
                client.DefaultRequestVersion = HttpVersion.Version11;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
            });

            options.HttpMessageHandlerBuilderActions.Add(builder =>
            {
                ForceHttp11MessageHandler.RestrictToTls12(builder.PrimaryHandler);

                if (builder.AdditionalHandlers.Any(handler => handler is ForceHttp11MessageHandler))
                    return;

                builder.AdditionalHandlers.Insert(0, new ForceHttp11MessageHandler());
            });
        });

        return services;
    }
}
