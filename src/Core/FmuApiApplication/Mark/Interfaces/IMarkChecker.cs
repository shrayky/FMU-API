using FmuApiApplication.Mark.Models;
using FmuApiDomain.Mark.Interfaces;
using FmuApiDomain.TsPiot.Models;

namespace FmuApiApplication.Mark.Interfaces
{
    public interface IMarkChecker
    {
        Task<MarkCheckResult> FmuApiDatabaseCheck(string sgtin, IMarkStateManager stateManager);
        
        Task<MarkCheckResult> OnlineCheck(string code, string sgtin, bool codeIsSgtin, int printGroupCode);
        
        Task<MarkCheckResult> OfflineCheck(string code, int printGroupCode, int trueApiGroupId);
        
        Task<MarkCheckResult> TsPiotCheck(string code, TsPiotConnectionSettings tsPiotConnectionSettings);
    }
}
