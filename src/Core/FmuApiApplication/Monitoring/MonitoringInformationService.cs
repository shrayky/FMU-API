using FmuApiApplication.LocalModule;
using FmuApiApplication.Monitoring.Dto;
using FmuApiApplication.Monitoring.Interfaces;
using FmuApiApplication.Statistics;
using FmuApiApplication.Statistics.Interfaces;
using FmuApiApplication.TsPiot;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Mark.Interfaces;
using FmuApiDomain.State.Interfaces;
using FmuApiDomain.Statistics.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FmuApiApplication.Monitoring;

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
    private async Task<MarkChecksStatistics> ColleсtStatistics()
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
