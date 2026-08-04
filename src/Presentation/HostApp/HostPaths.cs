namespace HostApp;

/// <summary>
/// Пути host: каталог установки и ProgramData.
/// </summary>
internal static class HostPaths
{
    /// <summary>
    /// Каталог, где лежит fmu-api.exe (host).
    /// </summary>
    public static string InstallRoot
    {
        get
        {
            var processPath = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(processPath))
                return Path.GetDirectoryName(processPath) ?? AppContext.BaseDirectory;

            return AppContext.BaseDirectory;
        }
    }

    /// <summary>
    /// Каталог данных host в ProgramData.
    /// </summary>
    public static string DataFolder =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            HostConstants.Manufacture,
            HostConstants.AppName);

    /// <summary>
    /// Каталог логов host.
    /// </summary>
    public static string LogFolder => Path.Combine(DataFolder, "host-log");
}
