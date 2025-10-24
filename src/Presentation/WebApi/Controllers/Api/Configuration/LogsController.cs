using LogService;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.Configuration
{
    [Route("api/configuration/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "App configuration")]
    public class LogsController : ControllerBase
    {
        [HttpGet("{selectedFileName}")]
        public async Task<IActionResult> LogsAsync(string selectedFileName)
            => Ok(await LogInformationPacket.CollectLogs(selectedFileName));
        
    }
}
