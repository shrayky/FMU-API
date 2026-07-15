using FmuApiDomain.Constants;
using FmuApiDomain.Fmu.FmuState;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Fmu.Status;

[Route("api/fmu/api4/system/[controller]")]
[Route("{inn}/api/fmu/api4/system/[controller]")]
[Route("api4/system/[controller]")]
[Route("{inn}/api4/system/[controller]")]
[Route("api4/system/get_info")]
[Route("{inn}/api4/system/get_info")]
[ApiController]
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
