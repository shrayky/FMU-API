using FmuApiDomain.Configuration;
using Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.Configuration
{
    [Route("api/configuration/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "App configuration")]
    public class XapiKeyController : Controller
    {
        private readonly IParametersService _parametersService;
        private readonly Parameters _configuration;

        public XapiKeyController(IParametersService parametersService)
        {
            _parametersService = parametersService;

            _configuration = _parametersService.Current();
        }

        [HttpGet]
        public IActionResult XApiKeyGet(IParametersService parametersService)
        {
            return Ok(_configuration.OrganisationConfig.XapiKey());
        }

        [HttpPost]
        async public Task<IActionResult> XApiKeyPostAsync(string xapi)
        {
            _configuration.OrganisationConfig.SetXapiKey(xapi);

            await _parametersService.UpdateAsync(_configuration);

            return Ok();
        }
    }
}
