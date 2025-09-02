using FmuApiDomain.DaemonsManager;
using ServicesAndDaemonsManager.Managers;

namespace ServicesAndDaemonsManager;

public static class ServiceAndDaemonsManagerFactory
{
    public static IDaemonManager Create()
    {
        if (OperatingSystem.IsWindows())
            return new WindowsServiceManager();

        if (OperatingSystem.IsLinux())
            return new LinuxDaemonManager();
        
        throw new Exception("Нет менеджера служб для текущей ОС");
    }
}