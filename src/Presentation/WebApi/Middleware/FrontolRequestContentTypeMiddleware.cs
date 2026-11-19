namespace WebApi.Middleware;

/// <summary>
/// Подставляет Content-Type для запросов Frontol без заголовка.
/// Форматтер JSON без этого заголовка отвечает 415 и тело не читает.
/// </summary>
public class FrontolRequestContentTypeMiddleware
{
    private static readonly string[] PathMarkers =
    [
        "/document",
        "/beer/connect_keg",
        "/products_by_stamp",
        "/api4/system/health",
        "/api4/system/get_info",
    ];

    private readonly RequestDelegate _next;
    private readonly ILogger<FrontolRequestContentTypeMiddleware> _logger;

    public FrontolRequestContentTypeMiddleware(
        RequestDelegate next,
        ILogger<FrontolRequestContentTypeMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method) && IsFrontolPath(context.Request.Path))
        {
            _logger.LogDebug(
                "Входящий POST {Path}. Content-Type: {ContentType}. Content-Length: {ContentLength}",
                context.Request.Path.Value,
                string.IsNullOrEmpty(context.Request.ContentType) ? "(нет)" : context.Request.ContentType,
                context.Request.ContentLength);

            if (string.IsNullOrEmpty(context.Request.ContentType))
                context.Request.ContentType = "application/json";
        }

        await _next(context);
    }

    private static bool IsFrontolPath(PathString path)
    {
        var value = path.Value;
        if (string.IsNullOrEmpty(value))
            return false;

        foreach (var marker in PathMarkers)
        {
            if (ContainsPath(value, marker))
                return true;
        }

        return false;
    }

    private static bool ContainsPath(string path, string marker)
    {
        var index = path.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return false;

        var end = index + marker.Length;
        return end == path.Length || path[end] == '/';
    }
}
