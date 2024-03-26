using System.Text.Json;
using System.Text.Json.Serialization;

namespace FmuApiDomain.Models.Configuration
{
    public class Parametrs : ICloneable
    {
        public string AppName { get; } = "FMU-API";
        public int AppVersion { get; set; } = 8;
        public string XAPIKEY { get; set; } = string.Empty;
        public ServerConfig ServerConfig { get; set; } = new();
        public AlcoUnitConfig FrontolAlcoUnit { get; set; } = new();
        public string HostToPing { get; set; } = "https://mail.ru";
        public List<TrueSignCdn> Cdn { get; set; } = new();
        public CouchDbConnection MarksDb { get; set; } = new();
        public TrueSignTokenService TrueSignTokenService { get; set; } = new();
        public HttpRequestTimeouts HttpRequestTimeouts { get; set; } = new();
        public LogSettings Logging { get; set; } = new();
        [JsonIgnore]
        public SignData SignData { get; set; } = new();

        private string _dataFolder = string.Empty;

        public void Init(string dataFolder)
        { 
            _dataFolder = dataFolder;

            LoadFromJson();
        }

        private void LoadFromJson()
        {
            string configFileName = $"{_dataFolder}\\config.json";

            if (!Directory.Exists(_dataFolder))
                Directory.CreateDirectory(_dataFolder);

            Parametrs loadedConstants = new Parametrs();

            if (File.Exists(configFileName))
                loadedConstants = LoadFromFile(configFileName);
            else
                Save(loadedConstants, configFileName);

            if (loadedConstants != null)
            {
                if (loadedConstants.AppVersion != AppVersion)
                {
                    loadedConstants.AppVersion = AppVersion;
                    Save(loadedConstants, configFileName);
                }

                XAPIKEY = loadedConstants.XAPIKEY;
                ServerConfig = loadedConstants.ServerConfig;
                FrontolAlcoUnit = loadedConstants.FrontolAlcoUnit;
                Cdn = loadedConstants.Cdn;
                MarksDb = loadedConstants.MarksDb;
                HostToPing = loadedConstants.HostToPing;
                TrueSignTokenService = loadedConstants.TrueSignTokenService;
                HttpRequestTimeouts = loadedConstants.HttpRequestTimeouts;
                Logging = loadedConstants.Logging;
            }

        }

        public void Save(Parametrs constantsToSave, string configFileName)
        {
            JsonSerializerOptions jsonOptions = new()
            {
                WriteIndented = true
            };

            string configJson = JsonSerializer.Serialize(constantsToSave, jsonOptions);

            try
            {
                StreamWriter file = new(configFileName, false);
                file.Write(configJson);
                file.Close();
            }
            catch
            { }
        }

        async public Task<bool> SaveAsync(Parametrs constantsToSave, string dataFolder)
        {
            string configFileName = $"{dataFolder}\\config.json";

            JsonSerializerOptions jsonOptions = new JsonSerializerOptions();
            jsonOptions.WriteIndented = true;

            using MemoryStream stream = new();
            await JsonSerializer.SerializeAsync(stream, constantsToSave, constantsToSave.GetType(), jsonOptions);

            stream.Position = 0;
            using var reader = new StreamReader(stream);
            string configJson = reader.ReadToEnd();

            try
            {
                StreamWriter file = new(configFileName, false);
                await file.WriteAsync(configJson);
                file.Close();
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static Parametrs LoadFromFile(string configFileName)
        {
            Parametrs? constant = new Parametrs();

            StreamReader file = new(configFileName);

            string configJson = file.ReadToEnd();
            file.Close();

            if (configJson != null)
                constant = JsonSerializer.Deserialize<Parametrs>(configJson);

            if (constant == null)
                return new Parametrs();

            return constant;
        }
        public object Clone() => MemberwiseClone();
    }
}
