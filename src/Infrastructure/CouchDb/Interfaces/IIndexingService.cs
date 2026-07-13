using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Options;

namespace CouchDb.Interfaces;

public interface IIndexingService
{
    Task<Result> EnsureIndexesExist(CouchDbConnection connection, CancellationToken  cancellationToken);
}