using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.MarkInformation.Models;
using FmuApiDomain.State.Interfaces;
using FmuApiDomain.ViewData.ApplicationMonitoring.Dto;
using FmuApiDomain.ViewData.ApplicationMonitoring.Interfaces;
using FmuApiDomain.ViewData.Dto;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace FmuApiDomain.ViewData.ApplicationMonitoring.Services;

[AutoRegisterService(ServiceLifetime.Transient)]
public class MonitoringInformationService : IMonitoringInformation
{
    private readonly IApplicationState _applicationState;
    private readonly IParametersService _parametersService;
    private readonly IMarkStatisticsService _markStatisticsService;
    private readonly IMemoryCache _cache;

    private const string Key7days = "check-marks-last-7";
    private const string Key30days = "check-marks-last-30";

    public MonitoringInformationService(IApplicationState applicationState, IParametersService parametersService,
        IMarkStatisticsService markStatisticsService, IMemoryCache cache)
    {
        _applicationState = applicationState;
        _parametersService = parametersService;
        _markStatisticsService = markStatisticsService;
        _cache = cache;
    }

    public async Task<MonitoringData> Collect()
    {
        var currentSettings = await _parametersService.CurrentAsync();
        
        return new MonitoringData()
        {
            CouchDbOnLine = currentSettings.Database.Enable
                ? (_applicationState.CouchDbOnline() ? "On-line" : "Off-line")
                : "Disabled",
            StateOfLocalModules = await LoadStateOfLocalModules(),
            MarkCheksStatistics = await CollccetStatisctics()
        };
    }

    private async Task<List<LocalModuleState>> LoadStateOfLocalModules()
    {
        var currentSettings = await _parametersService.CurrentAsync();

        List<LocalModuleState> stateOfLocalModules = [];

        foreach (var printGroup in currentSettings.OrganisationConfig.PrintGroups.Where(printGroup => printGroup.LocalModuleConnection.Enable))
        {
            var fullStateInfo = _applicationState.LocalModuleInformation(printGroup.Id);
            
            LocalModuleState lmState = new()
            {
                Address = printGroup.LocalModuleConnection.ConnectionAddress,
                Version = fullStateInfo.Version,
                LastSyncTime = fullStateInfo.LastSyncDateTime,
                State = fullStateInfo.StatusRaw,
                IsReady = fullStateInfo.IsReady,
            };
            
            stateOfLocalModules.Add(lmState);
            
        }
        return stateOfLocalModules;
    }

    private async Task<MarkCheksStatistics> CollccetStatisctics()
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
            
            Last7Days = await RestoreCachedStatistic(Key7days, 7),
            Last30Days = await RestoreCachedStatistic(Key30days, 30),
        };
    }
    
    private async Task<MarkChecksInformation> RestoreCachedStatistic(string cacheKey, int days)
    {
        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);
        var endOfDay = DateTime.UtcNow.Date.AddDays(1);
    
        var result = (await _cache.GetOrCreateAsync<MarkChecksInformation>(cacheKey, async cacheEntry =>
        {
            cacheEntry.AbsoluteExpiration = endOfDay;
        
            // Получаем данные за период до вчерашнего дня
            var fromDate = yesterday.AddDays(-days);

            MarkCheckStatistics data;
            
            try
            {
                data = await _markStatisticsService.ByDays(fromDate, yesterday);
            }
            catch (Exception e)
            {
                return new MarkChecksInformation();
            }

            return new MarkChecksInformation()
            {
                Total = data.Total,
                SuccessfulOffline = data.SuccessfulOfflineChecks,
                SuccessfulOnline = data.SuccessfulOnlineChecks,
                SuccessRate = data.SuccessRatePercentage
            };
        }));

        return result ?? new MarkChecksInformation();
    }
    
}