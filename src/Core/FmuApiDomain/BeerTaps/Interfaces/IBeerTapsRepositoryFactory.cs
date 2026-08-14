namespace FmuApiDomain.BeerTaps.Interfaces;

public interface IBeerTapsRepositoryFactory
{
    IDisposableBeerTapsRepository Create(string connectionString);
}

public interface IDisposableBeerTapsRepository : IBeerTapsRepository, IDisposable
{

}
