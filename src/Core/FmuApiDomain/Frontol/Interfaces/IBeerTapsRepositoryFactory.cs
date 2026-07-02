namespace FmuApiDomain.Frontol.Interfaces;

public interface IBeerTapsRepositoryFactory
{
    IDisposableBeerTapsRepository Create(string connectionString);
}

public interface IDisposableBeerTapsRepository : IBeerTapsRepository, IDisposable
{

}
