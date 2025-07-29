using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.LocalModule.Models;
using FmuApiDomain.State.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.Monitoring
{
    [Route("api/monitoring/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "App configuration")]
    public class SystemStateController : ControllerBase
    {
        private IApplicationState _applicationState;
        private IParametersService _parametersService;

        public SystemStateController(IApplicationState applicationState, IParametersService parametersService)
        {
            _applicationState = applicationState;
            _parametersService = parametersService;
        }

        [HttpGet]
        public async Task<IActionResult> SystemStateInformationAsync()
        {
            Dictionary<string, LocalModuleState> localModulInformation = [];

            var currentSettings = await _parametersService.CurrentAsync();

            foreach (var printGroup in currentSettings.OrganisationConfig.PrintGroups)
            {
                if (!printGroup.LocalModuleConnection.Enable)
                    continue;
                localModulInformation.Add(printGroup.LocalModuleConnection.ConnectionAddress, _applicationState.LocalModuleInformation(printGroup.Id));
            }

            var stateInfo = new
            {
                CouchDbOnLine = currentSettings.Database.Enable ? (_applicationState.CouchDbOnline() ? "On-line" : "Off-line") : "Disabled",
                LocaleModulesInformation = localModulInformation
            };

            return Ok(stateInfo);
        }
    }
}
