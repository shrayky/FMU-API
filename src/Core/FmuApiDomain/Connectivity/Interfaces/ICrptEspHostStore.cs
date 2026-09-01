using FmuApiDomain.Connectivity.Models;

namespace FmuApiDomain.Connectivity.Interfaces;

public interface ICrptEspHostStore
{
    IReadOnlyList<CrptEspHost> Load();
}
