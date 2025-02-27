using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options.Organization;
using FmuApiDomain.LocalModule.Enums;
using FmuApiDomain.State.Interfaces;
using LocalModuleIntegration.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.LocaleModule
{
    [Route("api/lm/init")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "Locale module")]
    public class InitLocaleModuleController : ControllerBase
    {
        private readonly ILogger<InitLocaleModuleController> _logger;
        private readonly IParametersService _parametersService;
        private readonly IApplicationState _applicationState;
        private readonly ILocalModuleService _localModuleService;

        public InitLocaleModuleController(ILogger<InitLocaleModuleController> logger,
            IParametersService parametersService,
            IApplicationState applicationState,
            ILocalModuleService localModuleService)
        {
            _logger = logger;
            _parametersService = parametersService;
            _applicationState = applicationState;
            _localModuleService = localModuleService;
        }

        [HttpPost("{organizationId}")]
        public async Task<IActionResult> Initialize(int organizationId)
        {
            _logger.LogInformation("Начало инициализации ЛМ для организации {OrganizationId}", organizationId);

            Parameters parameters = await _parametersService.CurrentAsync();
            PrintGroupData? printGroup = parameters.OrganisationConfig.PrintGroups.FirstOrDefault(p => p.Id == organizationId);

            if (printGroup == null)
            {
                _logger.LogError("Ошибка при инициализации ЛМ для организации {OrganizationId} - не найдена организация", organizationId);
                return NotFound();
            }

            try
            {
                var initResult = await _localModuleService.InitializeAsync(printGroup.LocalModuleConnection, printGroup.XAPIKEY);

                if (!initResult)
                    return BadRequest();

                _applicationState.UpdateOrganizationLocalModuleStatus(printGroup.Id, LocalModuleStatus.Initialization);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при инициализации ЛМ для организации {OrganizationId}", organizationId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Ошибка при инициализации локального модуля",
                    error = ex.Message
                });
            }
            var state = await _localModuleService.StateAsync(printGroup.LocalModuleConnection);
            
            _applicationState.UpdateOrganizationLocalModuleStatus(organizationId, state.Status);

            return Ok();
        }

    }
}
