using FmuApiApplication.Mark.Models;

namespace FmuApiApplication.Mark.Interfaces
{
    public interface IMarkChecker
    {
        Task<MarkCheckResult> OfflineCheck(string sgtin, IMarkStateManager stateManager);
        Task<MarkCheckResult> OnlineCheck(string code, string sgtin, bool codeIsSgtin, int printGroupCode);
    }
}
