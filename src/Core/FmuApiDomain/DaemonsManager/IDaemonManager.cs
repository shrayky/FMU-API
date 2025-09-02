namespace FmuApiDomain.DaemonsManager
{
    public interface IDaemonManager
    {
        bool Restart(string daemonName);
        bool IsRunning(string daemonName);
        string Status(string daemonName);
    }
}
