using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options.Organization;
using FmuApiDomain.LocalModule.Models;
using FmuApiDomain.State.Interfaces;
using LocalModuleIntegration.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.LocaleModule
{
    [Route("api/lm/state")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "Locale module")]
    public class StateOfLocalModuleController : ControllerBase
    {
        private readonly ILogger<StateOfLocalModuleController> _logger;
        private readonly IParametersService _parametersService;
        private readonly IApplicationState _applicationState;
        private readonly ILocalModuleService _localModuleService;
        public StateOfLocalModuleController(
            ILogger<StateOfLocalModuleController> logger,
            IParametersService parametersService,
            IApplicationState applicationState,
            ILocalModuleService localModuleService)
        {
            _logger = logger;
            _parametersService = parametersService;
            _applicationState = applicationState;
            _localModuleService = localModuleService;
        }

        [HttpGet]
        public async Task<IActionResult> LocalModulesStates()
        {
            var parameters = await _parametersService.CurrentAsync();
            var states = new List<OrganizationLocalModuleState>();

            if (parameters.OrganisationConfig.PrintGroups.Count == 0)
                return Ok(states);

            foreach (var organization in parameters.OrganisationConfig.PrintGroups)
            {
                OrganizationLocalModuleState lmStateInformation = new()
                {
                    Organization = organization.Id,
                    Status = _applicationState.OrganizationLocalModuleStatus(organization.Id)
                };

                states.Add(lmStateInformation);
            }

            return Ok(states);
        }

        [HttpGet("{organizationId}")]
        public async Task<IActionResult> StateGet(int organizationId)
        {
            return await State(organizationId);
        }

        [HttpPost("{organizationId}")]
        public async Task<IActionResult> StatePost(int organizationId)
        {
            return await State(organizationId);
        }

        private async Task<IActionResult> State(int organizationId)
        {
            _logger.LogInformation("Статус локального модуля {OrganizationId}", organizationId);

            Parameters parameters = await _parametersService.CurrentAsync();
            PrintGroupData? printGroup = parameters.OrganisationConfig.PrintGroups.FirstOrDefault(p => p.Id == organizationId);

            if (printGroup == null)
            {
                _logger.LogError("Ошибка при получении статуса ЛМ для организации {OrganizationId} - не найдена организация", organizationId);
                return NotFound();
            }

            try
            {
                var status = await _localModuleService.StateAsync(printGroup.LocalModuleConnection);

                return Ok(status);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении статуса ЛМ для организации {OrganizationId}", organizationId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Ошибка при получении статус локального модуля",
                    error = ex.Message
                });
            }
        }

    }
}
