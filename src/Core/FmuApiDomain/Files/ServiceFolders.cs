using FmuApiDomain.Constants;

namespace FmuApiDomain.Files;

/// Каталоги службы.
public static class ServiceFolders
{
    /// Каталог данных службы: настройки, отметка об установленном обновлении.
    /// Windows — ProgramData, Linux — /var/lib.
    public static string DataFolder() =>
        Shared.FilesFolders.Folders.CommonApplicationDataFolder(
            ApplicationInformation.Manufacture,
            ApplicationInformation.AppName);
}
