using FmuApiApplication.Documents;
using FmuApiDomain.Fmu.Token;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.ItidaIMark;

[Route("[controller]")]
[ApiController]
[ApiExplorerSettings(GroupName = "Itida IMark API")]
public class ItidaImarkController : ControllerBase
{
    private readonly ILogger<ItidaImarkController> _logger;
    private readonly FrontolDocumentServiceFactory _factory;

    private readonly int tokenLifeMinutes = 60;

    public ItidaImarkController(ILogger<ItidaImarkController> logger, FrontolDocumentServiceFactory factory)
    {
        _logger = logger;
        _factory = factory;
    }

    [HttpPost("/passwordauth")]
    public async Task<IActionResult> Authorization()
    {
        DateTime expired = DateTime.Now.AddMinutes(tokenLifeMinutes).ToUniversalTime();

        AuthorizationAnswer authorizationAnswer = new()
        {
            Id = "Pos",
            Name = "",
            Expired = (int)expired.Subtract(DateTime.UnixEpoch).TotalSeconds,
            Signature = "fmu-api-sign"
        };

        return Ok(authorizationAnswer);
    }
}
