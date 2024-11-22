using FmuApiDomain.Configuration.Options.Organisation;
using FmuApiSettings;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.Configuration
{
    [Route("api/configuration/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "App configuration")]
    public class OrganisationConfigController : Controller
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(Constants.Parametrs.OrganisationConfig);
        }

        [HttpPost]
        async public Task<IActionResult> PostAsync(OrganisationConfigurution organisationConfigurution)
        {
            Constants.Parametrs.OrganisationConfig = organisationConfigurution;

            await Constants.Parametrs.SaveAsync(Constants.Parametrs, Constants.DataFolderPath);

            return Ok();
        }
    }
}
