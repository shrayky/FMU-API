using TrueApiCdn.Models;

namespace TrueApiCdn.Interface
{
    public interface ICdnService
    {
        Task<IReadOnlyList<TrueSignCdn>> GetCdnsAsync();
        Task<TrueSignCdn?> GetActiveCdnAsync(int recursionCount);
        Task SaveCdnsAsync(IEnumerable<TrueSignCdn> cdns);
        Task UpdateCdnStatusAsync(string host, bool isOffline);
        Task ResetOfflineStatusAsync();
        void InvalidateCache();
    }
}
