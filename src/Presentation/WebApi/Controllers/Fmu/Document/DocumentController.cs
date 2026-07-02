using FmuApiApplication.Documents;
using FmuApiDomain.Fmu.Document;
using Microsoft.AspNetCore.Mvc;
using WebApi.Services;

namespace WebApi.Controllers.Fmu.Document;

[Route("api/fmu/[controller]")]
[Route("[controller]")]
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
}
