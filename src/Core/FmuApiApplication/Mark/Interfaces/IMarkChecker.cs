using FmuApiApplication.Mark.Models;
using FmuApiDomain.Mark.Entities;
using FmuApiDomain.TsPiot.Models;

namespace FmuApiApplication.Mark.Interfaces
{
    public interface IMarkChecker
    {
        MarkCheckResult FmuApiDatabaseCheck(string sgtin, MarkEntity markInfo);
        
        Task<MarkCheckResult> OnlineCheck(string code, string sgtin, bool codeIsSgtin, int printGroupCode);
        
        Task<MarkCheckResult> OfflineCheck(string code, int printGroupCode, int trueApiGroupId);
        
        Task<MarkCheckResult> TsPiotCheck(string code, TsPiotConnectionSettings tsPiotConnectionSettings);
    }
}
