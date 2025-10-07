namespace Shared.FilesFolders
{
    public static class Folders
    {
        public static string LogFolder(string manufacture, string appName)
        {
            var logFolder = string.Empty;

            if (OperatingSystem.IsWindows())
            {
                logFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                logFolder = Path.Combine(logFolder, manufacture, appName, "log");
            }
            else if (OperatingSystem.IsLinux())
            {
                logFolder = "/var/log";
                logFolder = Path.Combine(logFolder, appName);
            }

            return logFolder;
        }

        public static string CommonApplicationDataFolder(string manufacture, string appName)
        {
            var configFolder = string.Empty;

            if (OperatingSystem.IsWindows())
            {
                configFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                                           manufacture,
                                           appName);
            }
            else if (OperatingSystem.IsLinux())
            {
                configFolder = Path.Combine("/var/lib", appName);
            }

            return configFolder;
        }
        
        public static void CopyDirectory(string sourceDir, string targetDir)
        {
            var dir = new DirectoryInfo(sourceDir);
    
            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);
    
            foreach (var file in dir.GetFiles())
            {
                var targetFilePath = Path.Combine(targetDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }
    
            foreach (var subDir in dir.GetDirectories())
            {
                var targetSubDir = Path.Combine(targetDir, subDir.Name);
                CopyDirectory(subDir.FullName, targetSubDir);
            }
        }
    }
}
