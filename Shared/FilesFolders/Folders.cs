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
    }
}
