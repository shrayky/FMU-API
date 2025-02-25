using FmuApiDomain.Cdn;

namespace TrueApiCdn.Interface
{
    public interface ICdnService
    {
        Task<IReadOnlyList<TrueSignCdn>> GetCdnsAsync();
        Task<TrueSignCdn?> GetActiveCdnAsync(int recursionCount);
        Task SaveCdnsAsync(IEnumerable<TrueSignCdn> cdns);
        Task UpdateCdnStatusAsync(string host, bool isOffline);
        Task ResetOfflineStatusAsync();
        bool ShouldUpdateCdnList();
        void InvalidateCache();
    }
}
