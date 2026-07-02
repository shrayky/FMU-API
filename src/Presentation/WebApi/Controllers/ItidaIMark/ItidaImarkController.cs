using FmuApiApplication.Documents;
using FmuApiDomain.Constants;
using FmuApiDomain.Fmu.Token;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.ItidaIMark;

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
            Id = "itida-i-mark",
            Name = "",
            Expired = (int)expired.Subtract(DateTime.UnixEpoch).TotalSeconds,
            Signature = "fmu-api-sign"
        };

        return Ok(authorizationAnswer);
    }

    [HttpGet("/scripts")]
    public IActionResult FmuApiState()
    {
        return Ok(new
        {
            version = $"{ApplicationInformation.AppVersion}.{ApplicationInformation.Assembly}",
            skipcischeck = false,
            cdninterval = 8,
            dbtype = "couchdb",
            backupdate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
        });
    }

    [HttpGet("/settings")]
    public IActionResult FmuApiSettings()
    {
        var data = new
        {
            version = $"{ApplicationInformation.AppVersion}.{ApplicationInformation.Assembly}",
            skipcischeck = false,
            cdninterval = 8,
            dbtype = "couchdb",
            backupdate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
        };

        return Ok(new { data = data });
    }
}
