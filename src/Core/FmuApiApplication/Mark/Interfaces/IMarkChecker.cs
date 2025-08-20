using FmuApiApplication.Mark.Models;
using FmuApiDomain.MarkInformation.Interfaces;

namespace FmuApiApplication.Mark.Interfaces
{
    public interface IMarkChecker
    {
        Task<MarkCheckResult> FmuApiDatabaseCheck(string sgtin, IMarkStateManager stateManager);
        Task<MarkCheckResult> OnlineCheck(string code, string sgtin, bool codeIsSgtin, int printGroupCode);
        Task<MarkCheckResult> OfflineCheckAsync(string code, int printGroupCode);
    }
}
