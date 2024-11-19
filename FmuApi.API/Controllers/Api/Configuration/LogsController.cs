using LogService;
using Microsoft.AspNetCore.Mvc;

namespace FmuApiAPI.Controllers.Api.Configuration
{
    [Route("api/configuration/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "App configuration")]
    public class LogsController : ControllerBase
    {
        [HttpGet("{selectedFileName}")]
        public async Task<IActionResult> LogsAsync(string selectedFileName)
        {
            return Ok(await LogInformationPacket.CollectLogs(selectedFileName));
        }
    }
}
