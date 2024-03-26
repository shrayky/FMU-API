using FmuApiDomain.Models.Configuration;

namespace FmuApiSettings
{
    public static class Constants
    {
        public static Parametrs Parametrs { get; set; } = new Parametrs();
        public static string DataFolderPath { get; set; } = string.Empty;
        public static bool Online { get; set; } = true;

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
            ConfigurateDataFolder("");

            Parametrs.Init(DataFolderPath);
        }

        public static void Init(string dataFolder) 
        {
            ConfigurateDataFolder(dataFolder);

            Parametrs.Init(DataFolderPath);
        }

    }
}
