using FmuApiDomain.Models.Configuration;
using FmuApiDomain.Models.Configuration.TrueSign;

namespace FmuApiSettings
{
    public static class Constants
    {
        public static Parametrs Parametrs { get; set; } = new Parametrs();
        public static CdnData Cdn { get; set; } = new();
        public static string DataFolderPath { get; set; } = string.Empty;
        public static bool Online { get; set; } = true;
        public static SignData TrueApiToken { get; set; } = new();
        public static SignData FmuToken { get; set; } = new();

        private static void ConfigurateDataFolder(string _dataFloderPath)
        {
            if (_dataFloderPath != string.Empty)
            {
                DataFolderPath = _dataFloderPath;
                return;
            }

            if (OperatingSystem.IsWindows())
            {
                DataFolderPath = string.Concat(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "\\Automation\\", Parametrs.AppName);
            }
        }

        private static void LogFolderCheck()
        {
            string path = string.Concat(DataFolderPath, "\\log");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public static void Init()
        {
            Init("");
        }

        public static void Init(string dataFolder) 
        {
            ConfigurateDataFolder(dataFolder);

            LogFolderCheck();

            Parametrs.Init(DataFolderPath);
            Cdn.LoadFromFile(DataFolderPath);
        }

    }
}
