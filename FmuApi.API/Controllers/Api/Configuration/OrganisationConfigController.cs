using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Options.Organisation;
using Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.Configuration
{
    [Route("api/configuration/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "App configuration")]
    public class OrganisationConfigController : Controller
    {
        private readonly IParametersService _parametersService;
        private readonly Parameters _configuration;

        public OrganisationConfigController(IParametersService parametersService)
        {
            _parametersService = parametersService;
            _configuration = _parametersService.Current();
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(_configuration.OrganisationConfig);
        }

        [HttpPost]
        async public Task<IActionResult> PostAsync(OrganisationConfigurution organisationConfigurution)
        {
            _configuration.OrganisationConfig = organisationConfigurution;

            await _parametersService.UpdateAsync(_configuration);

            return Ok();
        }
    }
}
