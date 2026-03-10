using FmuApiDomain.State.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.System;

[Route("api/system/[controller]")]
[ApiController]
[ApiExplorerSettings(GroupName = "System")]
public class SystemActionsController : ControllerBase
{
    private readonly ILogger<SystemActionsController> _logger;
    private readonly IApplicationState _applicationState;

    public SystemActionsController(IApplicationState applicationState, ILogger<SystemActionsController> logger)
    {
        _applicationState = applicationState;
        _logger = logger;
    }

    [HttpPost]
    [Route("/api/system/reboot")]
    public IActionResult Reboot()
    {
        _logger.LogCritical("Получена команда по api на перезагрузку сервиса.");

        _applicationState.NeedRestartService(true);

        return Ok();
    }
    
}
