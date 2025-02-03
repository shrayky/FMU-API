using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Configuration.Options.TrueSign;
using FmuApiDomain.TrueApiCdn;

namespace FmuApiSettings
{
    public static class Constants
    {
        public static Parameters Parameters { get; set; } = new Parameters();
        //public static ICdnRepository Cdn { get; set; } = new CdnData();
        public static string DataFolderPath { get; set; } = string.Empty;
        public static bool Online { get; set; } = true;
        public static SignData TrueApiToken { get; set; } = new();
        public static SignData FmuToken { get; set; } = new();

        private static void ConfigurateDataFolder(string _dataFolderPath)
        {
            if (_dataFolderPath != string.Empty)
            {
                DataFolderPath = _dataFolderPath;
                return;
            }

            if (OperatingSystem.IsWindows())
            {
                DataFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Automation", Parameters.AppName);
            }
            else if (OperatingSystem.IsLinux())
            {
                DataFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Automation", Parameters.AppName);
            }
        }

        private static void LogFolderCheck()
        {
            string path = Path.Combine(DataFolderPath, "log");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public static void Init()
        {
            Init("");
        }

        public static void Init(string dataFolder) 
        {
            //ConfigurateDataFolder(dataFolder);
            //LogFolderCheck();
            //Parameters.Init(DataFolderPath);

            //Cdn.LoadFromFile(DataFolderPath);
        }

    }
}
