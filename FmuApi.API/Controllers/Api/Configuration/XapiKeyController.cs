using FmuApiApplication;
using FmuApiSettings;
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
            return Ok(Constants.Parametrs.OrganisationConfig.XapiKey());
        }

        [HttpPost]
        async public Task<IActionResult> XApiKeyPostAsync(string xapi)
        {
            Constants.Parametrs.OrganisationConfig.SetXapiKey(xapi);

            await Constants.Parametrs.SaveAsync(Constants.Parametrs, Constants.DataFolderPath);

            return Ok();
        }
    }
}
