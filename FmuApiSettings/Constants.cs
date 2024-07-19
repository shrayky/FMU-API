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

        public static void Init()
        {
            Init("");
        }

        public static void Init(string dataFolder) 
        {
            ConfigurateDataFolder(dataFolder);

            Parametrs.Init(DataFolderPath);
            Cdn.LoadFromFile(DataFolderPath);
        }

    }
}
