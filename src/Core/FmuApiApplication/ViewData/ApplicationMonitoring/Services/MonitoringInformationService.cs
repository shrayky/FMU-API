using FmuApiApplication.Services.Statistics;
using FmuApiApplication.Services.Statistics.Interfaces;
using FmuApiApplication.StateCollectors;
using FmuApiApplication.ViewData.ApplicationMonitoring.Dto;
using FmuApiApplication.ViewData.ApplicationMonitoring.Interfaces;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.State.Interfaces;
using FmuApiDomain.ViewData.ApplicationMonitoring.Dto;
using FmuApiDomain.ViewData.Dto;
using Microsoft.Extensions.DependencyInjection;

namespace FmuApiApplication.ViewData.ApplicationMonitoring.Services;

[AutoRegisterService(ServiceLifetime.Transient)]
public class MonitoringInformationService : IMonitoringInformation
{
    private readonly IApplicationState _applicationState;
    private readonly IParametersService _parametersService;
    private readonly IMarkStatisticsService _markStatisticsService;
    private readonly ICachedMarkStatisticsProvider _cachedMarkStatisticsProvider;

    public MonitoringInformationService(
        IApplicationState applicationState,
        IParametersService parametersService,
        IMarkStatisticsService markStatisticsService,
        ICachedMarkStatisticsProvider cachedMarkStatisticsProvider)
    {
        _applicationState = applicationState;
        _parametersService = parametersService;
        _markStatisticsService = markStatisticsService;
        _cachedMarkStatisticsProvider = cachedMarkStatisticsProvider;
    }

    public async Task<MonitoringData> Collect()
    {
        var currentSettings = await _parametersService.CurrentAsync();

        return new MonitoringData()
        {
            CouchDbOnLine = DatabaseOnline(currentSettings),
            StateOfLocalModules = LmStateCollector.Collect(currentSettings, _applicationState),
            MarkCheksStatistics = await ColleсtStatistics(),
            TsPiotStates = TsPiotStateCollector.Collect(currentSettings, _applicationState)
        };
    }

    private string DatabaseOnline(Parameters parameters)
        => parameters.Database.Enable
                ? (_applicationState.CouchDbOnline() ? "On-line" : "Off-line")
                : "Disabled";
    private async Task<MarkCheksStatistics> ColleсtStatistics()
    {
        var todayRaw = await _markStatisticsService.Today();

        return new()
        {
            Today = new MarkChecksInformation
            {
                Total = todayRaw.Total,
                SuccessfulOffline = todayRaw.SuccessfulOfflineChecks,
                SuccessfulOnline = todayRaw.SuccessfulOnlineChecks,
                SuccessRate = todayRaw.SuccessRatePercentage
            },

            Last7Days = await _cachedMarkStatisticsProvider.RestoreCachedStatistic(
                CachedMarkStatisticsProvider.Key7days, 7),

            Last30Days = await _cachedMarkStatisticsProvider.RestoreCachedStatistic(
                CachedMarkStatisticsProvider.Key30days, 30),
        };
    }
}
