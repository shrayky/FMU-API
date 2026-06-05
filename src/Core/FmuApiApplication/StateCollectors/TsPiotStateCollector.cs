using FmuApiApplication.StateCollectors.Models;
using FmuApiDomain.Configuration;
using FmuApiDomain.State.Interfaces;

namespace FmuApiApplication.StateCollectors;

public static class TsPiotStateCollector
{
    public static List<TsPiotStateInfotmation> Collect(Parameters settings, IApplicationState appState)
    {
        if (!settings.ServerConfig.TsPiotEnabled)
            return [];

        List<TsPiotStateInfotmation> modules = [];

        var printGroups = settings.OrganisationConfig.PrintGroups;

        foreach (var pg in printGroups)
        {
            if (string.IsNullOrEmpty(pg.TsPiot.Host) || string.IsNullOrEmpty(pg.TsPiot.Port))
                continue;

            var address = $"{pg.TsPiot.Host}:{pg.TsPiot.Port}";

            var state = new TsPiotStateInfotmation()
            {
                Address = address,
                Name = pg.Name,
                ProtocolVersion = appState.TsPiotApiVersion(address),
                Online = appState.TsPiotIsOnline(address),
                LastCheckTime = appState.TsPiotLastSee(address),
                Version = appState.TsPiotModuleVersion(address),
            };

            modules.Add(state);
        }

        return modules;
    }
}
