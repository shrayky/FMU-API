using FmuApiApplication;
using Microsoft.AspNetCore.Mvc;

namespace FmuApiAPI.Controllers.Api.Configuration
{
    [Route("api/configuration/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "App configuration")]
    public class XapiKeyController : Controller
    {
        [HttpGet]
        public IActionResult XApiKeyGet()
        {
            return Ok(Constants.Parametrs.XAPIKEY);
        }

        [HttpPost]
        async public Task<IActionResult> XApiKeyPost(string xapi)
        {
            Constants.Parametrs.XAPIKEY = xapi;
            await Constants.Parametrs.SaveAsync(Constants.Parametrs);

            return Ok();
        }
    }
}
