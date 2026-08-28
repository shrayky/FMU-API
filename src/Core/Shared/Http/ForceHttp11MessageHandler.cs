using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;

namespace Shared.Http;

/// <summary>
/// Принудительно HTTP/1.1 и TLS 1.2.
/// Нужно для Windows 7/8: Schannel не умеет ALPN/HTTP/2 и TLS 1.3, .NET 10 иначе падает на SSL handshake.
/// </summary>
public sealed class ForceHttp11MessageHandler : DelegatingHandler
{
    /// <summary>
    /// Нужно ли ограничивать протоколы на текущей ОС.
    /// </summary>
    public static bool IsRequiredOnThisOs =>
        OperatingSystem.IsWindows() && !OperatingSystem.IsWindowsVersionAtLeast(10);

    public ForceHttp11MessageHandler()
    {
    }

    public ForceHttp11MessageHandler(HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
        RestrictToTls12(innerHandler);
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RestrictToHttp11(request);
        return base.SendAsync(request, cancellationToken);
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RestrictToHttp11(request);
        return base.Send(request, cancellationToken);
    }

    /// <summary>
    /// Запрещает HTTP/2 и выше на конкретном запросе.
    /// </summary>
    private static void RestrictToHttp11(HttpRequestMessage request)
    {
        request.Version = HttpVersion.Version11;
        request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
    }

    /// <summary>
    /// Включает только TLS 1.2 на внутреннем handler, не заменяя его.
    /// </summary>
    public static void RestrictToTls12(HttpMessageHandler handler)
    {
        switch (handler)
        {
            case SocketsHttpHandler sockets:
                sockets.SslOptions ??= new SslClientAuthenticationOptions();
                sockets.SslOptions.EnabledSslProtocols = SslProtocols.Tls12;
                break;
            case HttpClientHandler http:
                http.SslProtocols = SslProtocols.Tls12;
                break;
            case DelegatingHandler { InnerHandler: not null } nested:
                RestrictToTls12(nested.InnerHandler);
                break;
        }
    }
}
