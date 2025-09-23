namespace Shared.FilesFolders
{
    public static class Folders
    {
        public static string LogFolder()
        {
            var logFolder = string.Empty;

            if (OperatingSystem.IsWindows())
            {
                logFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            }
            else if (OperatingSystem.IsLinux())
            {
                logFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }

            return logFolder;
        }

        public static string CommonApplicationDataFolder(string manufacture, string appName)
        {
            string configFolder = string.Empty;

            if (OperatingSystem.IsWindows())
            {
                configFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                                           manufacture,
                                           appName);
            }
            else if (OperatingSystem.IsLinux())
            {
                configFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                                           manufacture,
                                           appName);
            }

            return configFolder;
        }
    }
}
