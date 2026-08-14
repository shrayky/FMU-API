using FmuApiDomain.Configuration.Options;

namespace FmuApiDomain.State.Interfaces
{
    public interface IStatusDbService
    {
        Task<bool> CheckAvailability(string databaseUrl, CancellationToken cancellationToken = default);
        Task<bool> EnsureDatabasesExists(CouchDbConnection connection, string[] databasesNames, CancellationToken cancellationToken);
    }
}
