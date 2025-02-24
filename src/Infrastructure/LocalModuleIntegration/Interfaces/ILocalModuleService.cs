using FmuApiDomain.Configuration.Options;
using FmuApiDomain.LocalModule.Models;
using FmuApiDomain.TrueApi.MarkData.Check;

namespace LocalModuleIntegration.Interfaces
{
    public interface ILocalModuleService
    {
        Task<bool> InitializeAsync(LocalModuleConnection connection, string xApiKey);
        Task<LocalModuleState> StateAsync(LocalModuleConnection connection);
        Task<CheckMarksDataTrueApi> OutCheckAsync(LocalModuleConnection connection, string cis, string xapiKey);
    }
}
