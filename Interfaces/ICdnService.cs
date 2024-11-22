using FmuApiDomain.Configuration.Options.TrueSign;

namespace Interfaces
{
    public interface ICdnService
    {
        Task<CdnData> CurrentAsync();
    }
}
