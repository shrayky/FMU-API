using FmuApiDomain.TrueApiIntegration.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.Configuration;

[Route("api/[controller]")]
[ApiController]
[ApiExplorerSettings(GroupName = "Digital signature")]
public class DigitalSignatureController : ControllerBase
{
    private IDigtalSignatureService _service;

    public DigitalSignatureController(IDigtalSignatureService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var data = _service.List();

        return Ok(data);
    }
}
