using FmuApiDomain.Configuration.Options;

namespace FmuApiDomain.Database.Interface
{
    public interface IStatusDbService
    {
        Task<bool> CheckAvailability(string databaseUrl, CancellationToken cancellationToken = default);
        Task<bool> EnsureDatabasesExists(CouchDbConnection connection, string[] databasesNames, CancellationToken cancellationToken);
    }
}
