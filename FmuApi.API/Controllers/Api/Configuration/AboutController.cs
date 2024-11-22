using FmuApiSettings;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.Configuration
{
    [Route("api/configuration/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "App configuration")]
    public class AboutController : ControllerBase
    {
        [HttpGet]
        public IActionResult AboutGet()
        {
            return Ok($"{Constants.Parametrs.AppName} version {Constants.Parametrs.AppVersion} assembly {Constants.Parametrs.Assembly}");
        }
    }
}
