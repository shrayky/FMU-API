using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.LocalModule.Models;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.State.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApi.Controllers.Api.Monitoring
{
    [Route("api/monitoring/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "App configuration")]
    public class SystemStateController : ControllerBase
    {
        private IApplicationState _applicationState;
        private IParametersService _parametersService;
        private IMarkStatisticsService _markStatisticsService;

        public SystemStateController(IApplicationState applicationState, IParametersService parametersService, IMarkStatisticsService markStatisticsService)
        {
            _applicationState = applicationState;
            _parametersService = parametersService;
            _markStatisticsService = markStatisticsService;
        }

        [HttpGet]
        public async Task<IActionResult> SystemStateInformationAsync()
        {
            Dictionary<string, LocalModuleState> localModelInformation = [];

            var currentSettings = await _parametersService.CurrentAsync();

            foreach (var printGroup in currentSettings.OrganisationConfig.PrintGroups.Where(printGroup => printGroup.LocalModuleConnection.Enable))
            {
                localModelInformation.Add(printGroup.LocalModuleConnection.ConnectionAddress, _applicationState.LocalModuleInformation(printGroup.Id));
            }

            var stateInfo = new
            {
                CouchDbOnLine = currentSettings.Database.Enable ? (_applicationState.CouchDbOnline() ? "On-line" : "Off-line") : "Disabled",
                LocaleModulesInformation = localModelInformation,
                CheckStatistics = currentSettings.Database.Enable ? await GetMarkStatisticsAsync() : new {}
            };

            return Ok(stateInfo);
        }

        private async Task<object> GetMarkStatisticsAsync()
        {
            var todayStats = await _markStatisticsService.Today();
            var last7DaysStats = await _markStatisticsService.LastWeek();
            var last30DaysStats = await _markStatisticsService.LastMonth();

            return new
            {
                Today = new
                {
                    Total = todayStats.Total,
                    SuccessfulOnline = todayStats.SuccessfulOnlineChecks,
                    SuccessfulOffline = todayStats.SuccessfulOfflineChecks,
                    SuccessRate = todayStats.SuccessRatePercentage
                },
                Last7Days = new
                {
                    Total = last7DaysStats.Total,
                    SuccessfulOnline = last7DaysStats.SuccessfulOnlineChecks,
                    SuccessfulOffline = last7DaysStats.SuccessfulOfflineChecks,
                    SuccessRate = last7DaysStats.SuccessRatePercentage
                }, 
                Last30Days = new
                {
                    Total = last30DaysStats.Total,
                    SuccessfulOnline = last30DaysStats.SuccessfulOnlineChecks,
                    SuccessfulOffline = last30DaysStats.SuccessfulOfflineChecks,
                    SuccessRate = last30DaysStats.SuccessRatePercentage
                }
            };
        }
    }
}
