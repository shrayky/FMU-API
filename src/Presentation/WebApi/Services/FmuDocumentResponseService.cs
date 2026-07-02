using System.Text;
using System.Text.Json;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Fmu.Document;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace WebApi.Services;

public class FmuDocumentResponseService
{
    private readonly IParametersService _parametersService;
    private readonly JsonSerializerOptions _jsonOptions;

    public FmuDocumentResponseService(
        IParametersService parametersService,
        IOptions<JsonOptions> jsonOptionsAccessor)
    {
        _parametersService = parametersService;
        _jsonOptions = jsonOptionsAccessor.Value.JsonSerializerOptions;
    }

    public IActionResult Ok(FmuAnswer answer) =>
        CreateJsonResult(answer, StatusCodes.Status200OK);

    public IActionResult BadRequest(object error) =>
        CreateJsonResult(error, StatusCodes.Status400BadRequest);

    private IActionResult CreateJsonResult(object value, int statusCode)
    {
        var encoding = _parametersService.Current().ServerConfig.ResponseEncoding;

        if (encoding == DocumentResponseEncoding.Utf8)
            return new JsonResult(value, _jsonOptions) { StatusCode = statusCode };

        var json = JsonSerializer.Serialize(value, _jsonOptions);
        var bytes = Encoding.GetEncoding(1251).GetBytes(json);

        return new EncodedJsonActionResult(statusCode, bytes);
    }
}

internal sealed class EncodedJsonActionResult(int statusCode, byte[] body) : IActionResult
{
    public async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        response.StatusCode = statusCode;
        response.ContentType = "application/json; charset=windows-1251";
        response.ContentLength = body.Length;
        await response.Body.WriteAsync(body);
    }
}
