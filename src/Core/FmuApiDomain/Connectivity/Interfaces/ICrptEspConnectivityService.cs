using FmuApiDomain.Connectivity.Models;

namespace FmuApiDomain.Connectivity.Interfaces;

public interface ICrptEspConnectivityService
{
    CrptEspCheckResult ListHosts();

    Task<CrptEspCheckResult> CheckAll(CancellationToken cancellationToken);
}
