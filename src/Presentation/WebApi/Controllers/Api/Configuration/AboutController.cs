using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.Configuration
{
    [Route("api/configuration/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "App configuration")]
    public class AboutController : ControllerBase
    {
        private readonly IParametersService _parametersService;
        private readonly Parameters _configuration;

        public AboutController(IParametersService parametersService)
        {
            _parametersService = parametersService;

            _configuration = _parametersService.Current();
        }

        [HttpGet]
        public IActionResult AboutGet()
        {
            return Ok($"{_configuration.AppName} version {_configuration.AppVersion} assembly {_configuration.Assembly}");
        }
    }
}
