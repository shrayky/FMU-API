using Microsoft.AspNetCore.Http;

namespace WebApi.Middleware;

/// <summary>
/// Принимает POST /document от Frontol без заголовка Content-Type.
/// Форматтер JSON без этого заголовка отвечает 415 и тело не читает.
/// </summary>
public class FrontolDocumentRequestLogMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FrontolDocumentRequestLogMiddleware> _logger;

    public FrontolDocumentRequestLogMiddleware(
        RequestDelegate next,
        ILogger<FrontolDocumentRequestLogMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method) && IsDocumentPath(context.Request.Path))
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

    private static bool IsDocumentPath(PathString path)
    {
        var value = path.Value;
        if (string.IsNullOrEmpty(value))
            return false;

        return value.EndsWith("/document", StringComparison.OrdinalIgnoreCase)
            || value.EndsWith("/document/", StringComparison.OrdinalIgnoreCase);
    }
}
