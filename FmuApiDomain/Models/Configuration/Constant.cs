using System.Text.Json;
using System.Text.Json.Serialization;

namespace FmuApiDomain.Models.Configuration
{
    public class Constant : ICloneable
    {
        public string AppName { get; } = "FMU-API";
        public int AppVersion { get; set; } = 6;
        public string XAPIKEY { get; set; } = string.Empty;
        public ServerConfig ServerConfig { get; set; } = new();
        public List<TrueSignCdn> Cdn { get; set; } = new();
        public CouchDbConnection CouchDb { get; set; } = new();
        public string HostToPing { get; set; } = "https://mail.ru";
        public TrueSignTokenService TrueSignTokenService { get; set; } = new();
        [JsonIgnore]
        public SignData SignData { get; set; } = new();

        public void Init()
        {
            LoadFromJson();
        }

        private void LoadFromJson()
        {
            string programDataFloder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string dataFolder = $"{programDataFloder}\\Automation\\{AppName}";
            string configFileName = $"{dataFolder}\\config.json";

            if (!Directory.Exists(dataFolder))
                Directory.CreateDirectory(dataFolder);

            Constant loadedConstants = new Constant();

            if (File.Exists(configFileName))
                loadedConstants = LoadFromFile(configFileName);
            else
                Save(configFileName, loadedConstants);

            if (loadedConstants != null)
            {
                if (loadedConstants.AppVersion != AppVersion)
                {
                    loadedConstants.AppVersion = AppVersion;
                    Save(configFileName, loadedConstants);
                }

                XAPIKEY = loadedConstants.XAPIKEY;
                ServerConfig = loadedConstants.ServerConfig;
                Cdn = loadedConstants.Cdn;
                CouchDb = loadedConstants.CouchDb;
                HostToPing = loadedConstants.HostToPing;
                TrueSignTokenService = loadedConstants.TrueSignTokenService;
            }

        }

        public void Save(string configFileName, Constant constantsToSave)
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

        async public Task<bool> SaveAsync(Constant constantsToSave)
        {
            string programDataFloder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string dataFolder = $"{programDataFloder}\\Automation\\{AppName}";
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

        private static Constant LoadFromFile(string configFileName)
        {
            Constant? constant = new Constant();

            StreamReader file = new(configFileName);

            string configJson = file.ReadToEnd();
            file.Close();

            if (configJson != null)
                constant = JsonSerializer.Deserialize<Constant>(configJson);

            if (constant == null)
                return new Constant();

            return constant;
        }
        public object Clone() => MemberwiseClone();
    }

}
