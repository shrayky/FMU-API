using FmuApiApplication.TrueApi;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.State.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.TrueSign;

[Route("api/ts/[controller]")]
[ApiController]
[ApiExplorerSettings(GroupName = "True API")]
public class TokenController : ControllerBase
{
    private readonly IApplicationState _applicationState;
    private readonly IParametersService _parametersService;

    public TokenController(IApplicationState applicationState, IParametersService parametersService)
    {
        _applicationState = applicationState;
        _parametersService = parametersService;
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

    [HttpGet("states")]
    public async Task<IActionResult> States()
    {
        var parameters = await _parametersService.CurrentAsync();
        return Ok(TrueApiTokenStateCollector.Collect(parameters, _applicationState));
    }
}
