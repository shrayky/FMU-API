using FmuApiApplication.ViewData.ApplicationMonitoring.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.Monitoring
{
    [Route("api/monitoring/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "App configuration")]
    public class SystemStateController : ControllerBase
    {
        private readonly IMonitoringInformation _monitoringInformation;

        public SystemStateController(IMonitoringInformation monitoringInformation)
        {
            _monitoringInformation = monitoringInformation;
        }

        [HttpGet]
        public async Task<IActionResult> FmuApiStateInformation()
            => Ok(await _monitoringInformation.Collect());
    }
}