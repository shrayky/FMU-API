using FmuApiDomain.State.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.TrueSign;

[Route("api/ts/[controller]")]
[ApiController]
[ApiExplorerSettings(GroupName = "True API")]
public class TokenController : ControllerBase
{
    private readonly IApplicationState _applicationState;

    public TokenController(IApplicationState applicationState)
    {
        _applicationState = applicationState;
    }

    [HttpGet]
    public IActionResult Token()
    {
        var data = _applicationState.TrueApiToken();

        if (data.Token == string.Empty)
            return NotFound();

        return Ok(data);
    }

    [HttpGet("inn")]
    public async Task<IActionResult> Token(string inn)
    {
        var data = _applicationState.TrueApiToken(inn);

        if (data.Token == string.Empty)
            return NotFound();

        return Ok(data);
    }
}
