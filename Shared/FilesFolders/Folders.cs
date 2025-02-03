namespace Shared.FilesFolders
{
    public static class Folders
    {
        public static string LogFolder()
        {
            var user = Environment.UserName;
            string logFolder = string.Empty;

            if (OperatingSystem.IsWindows())
            {
                logFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            }
            else if (OperatingSystem.IsLinux())
            {
                logFolder = Path.Combine("/var", "log");
            }

            return logFolder;
        }

        public static string CommonApplicationDataFolder(string Manufacture, string AppName)
        {
            string configFolder = string.Empty;

            if (OperatingSystem.IsWindows())
            {
                configFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                                           Manufacture,
                                           AppName);
            }
            else if (OperatingSystem.IsLinux())
            {
                configFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                                           Manufacture,
                                           AppName);
            }

            return configFolder;
        }
    }
}
