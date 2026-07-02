using FmuApiDomain.Constants;
using FmuApiDomain.Fmu.FmuState;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Fmu.Status;

[Route("api/fmu/api4/system/[controller]")]
[Route("api4/system/[controller]")]
[Route("api4/system/get_info")]
[ApiExplorerSettings(GroupName = "Frontol mark unit API")]
public class HealthController : ControllerBase
{
    [HttpPost]
    public IActionResult Health()
    {
        var state = new SystemHealth() 
        {
            Version = $"{ApplicationInformation.AppVersion}.{ApplicationInformation.Assembly}",
            Timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()
        };

        return Ok(state);
    }
}
