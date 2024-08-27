using FmuApiSettings;
using Microsoft.AspNetCore.Mvc;

namespace FmuApiAPI.Controllers.Api.Configuration
{
    [Route("api/configuration/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "App configuration")]
    public class AboutController : Controller
    {
        [HttpGet]
        public IActionResult XApiKeyGet()
        {
            return Ok($"{Constants.Parametrs.AppName} version {Constants.Parametrs.AppVersion} assembly {Constants.Parametrs.Assembly}");
        }
    }
}
