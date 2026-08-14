using FmuApiApplication.Services.Piot;
using FmuApiDomain.TsPiot.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.TsPiot;

[Route("api/tspiot")]
[ApiController]
[ApiExplorerSettings(GroupName = "TsPiot")]
public class TsPiotSettingsController(IPiotSettingsPushService pushService) : ControllerBase
{
    [HttpPost("settings")]
    public async Task<IActionResult> PushSettings(CancellationToken cancellationToken)
    {
        var result = await pushService.PushCurrentSettings(cancellationToken);
        if (result.IsFailure)
            return MapError(result.Error);

        return Ok(new
        {
            updated = result.Value.UpdatedInstances,
            failed = result.Value.FailedInstances,
            errors = result.Value.Errors
        });
    }

    private IActionResult MapError(string error) => error switch
    {
        PiotSettingsPushService.TsPiotDisabled => BadRequest(new { error }),
        _ => StatusCode(500, new { error })
    };
}
