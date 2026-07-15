using FmuApiApplication.Documents;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Token;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using WebApi.Services;

namespace WebApi.Controllers.Fmu.Document;

[Route("api/fmu/[controller]")]
[Route("api/fmu/{inn}/[controller]")]
[Route("[controller]")]
[Route("{inn}/[controller]")]
[ApiController]
[ApiExplorerSettings(GroupName = "Frontol mark unit API")]
public class DocumentController : ControllerBase
{
    private readonly ILogger<DocumentController> _logger;
    private readonly FrontolDocumentServiceFactory _factory;
    private readonly FmuDocumentResponseService _responseService;
    
    public DocumentController(
        ILogger<DocumentController> logger, 
        FrontolDocumentServiceFactory factory,
        FmuDocumentResponseService responseService)
    {
        _logger = logger;
        _factory = factory;
        _responseService = responseService;
    }

    [HttpPost]
    public async Task<IActionResult> DocumentPostAsync(RequestDocument document)
    {
        document.RequestFromAppId = AppIdFromToken(Request.Headers.Authorization.ToString());

        var service = _factory.GetInstance(document);

        if (service == null)
        {
            return BadRequest();
        }

        var result = await service.ActionAsync();

        if (result.IsFailure)
            return _responseService.BadRequest(result.Error);

        return _responseService.Ok(result.Value);
    }

    [HttpPost("inn")]
    public async Task<IActionResult> DocumentPostAsync(RequestDocument document, string inn)
    {
        document.RequestFromAppId = AppIdFromToken(Request.Headers.Authorization.ToString());

        foreach (var position in document.Positions)
        {
            position.Organisation.Inn = inn;
        }

        var service = _factory.GetInstance(document);

        if (service == null)
        {
            return BadRequest();
        }

        var result = await service.ActionAsync();

        if (result.IsFailure)
            return _responseService.BadRequest(result.Error);

        return _responseService.Ok(result.Value);
    }

    private string AppIdFromToken(string authHeader)
    {
        var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authHeader["Bearer ".Length..]
            : authHeader;

        if (string.IsNullOrEmpty(token))
            return "fmu";

        AuthorizationAnswer? authorizationData;
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            authorizationData = JsonSerializer.Deserialize<AuthorizationAnswer>(json);
        }
        catch (FormatException)
        {
            return "fmu";
        }

        if (authorizationData == null)
            return "fmu";

        if (authorizationData.Id != "pos")
            return authorizationData.Id;

        return "fmu";
    }
}
