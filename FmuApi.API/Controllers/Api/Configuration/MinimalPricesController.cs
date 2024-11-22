using FmuApiDomain.Configuration.Options;
using FmuApiSettings;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.Configuration
{
    [Route("api/configuration/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "App configuration")]
    public class MinimalPricesController : Controller
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(Constants.Parametrs.MinimalPrices);
        }

        [HttpPost]
        async public Task<IActionResult> PostAsync(MinimalPrices minimalPrices)
        {
            Constants.Parametrs.MinimalPrices = minimalPrices;

            await Constants.Parametrs.SaveAsync(Constants.Parametrs, Constants.DataFolderPath);

            return Ok();
        }
    }
}
