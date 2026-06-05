using FmuApiApplication.StateCollectors.Models;
using FmuApiDomain.Configuration;
using FmuApiDomain.State.Interfaces;

namespace FmuApiApplication.StateCollectors;

public static class LmStateCollector
{
    public static List<LocalModuleStateInformation> Collect(Parameters currentSettings, IApplicationState appState)
    {
        List<LocalModuleStateInformation> stateOfLocalModules = [];

        var printGroups = currentSettings.OrganisationConfig.PrintGroups;

        foreach (var printGroup in printGroups.Where(printGroup => printGroup.LocalModuleConnection.Enable))
        {
            var fullStateInfo = appState.LocalModuleInformation(printGroup.Id);

            LocalModuleStateInformation lmState = new()
            {
                Address = printGroup.LocalModuleConnection.ConnectionAddress,
                Version = fullStateInfo.Version,
                LastSyncTime = fullStateInfo.LastSyncDateTime,
                State = fullStateInfo.StatusRaw,
                IsReady = fullStateInfo.IsReady,
                
                Id = printGroup.Id,
                LastSync = fullStateInfo.LastSyncTimestamp,
                OperationMode = fullStateInfo.OperationModeRaw,
                Status = fullStateInfo.StatusRaw
            };

            stateOfLocalModules.Add(lmState);

        }
        return stateOfLocalModules;
    }
}
