using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Options;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.Configuration
{
    [Route("api/configuration/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "App configuration")]
    public class MinimalPricesController : Controller
    {
        private readonly IParametersService _parametersService;
        private readonly Parameters _configuration;

        public MinimalPricesController(IParametersService parametersService)
        {
            _parametersService = parametersService;

            _configuration = _parametersService.Current();
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(_configuration.MinimalPrices);
        }

        [HttpPost]
        async public Task<IActionResult> PostAsync(MinimalPrices minimalPrices)
        {
            _configuration.MinimalPrices = minimalPrices;

            await _parametersService.UpdateAsync(_configuration);

            return Ok();
        }
    }
}
