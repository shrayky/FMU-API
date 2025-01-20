using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Configuration.Options.Organisation;
using FmuApiDomain.Configuration.Options.TrueSign;
using JsonSerilizerOptions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FmuApiDomain.Configuration
{
    public class Parametrs : ICloneable
    {
        public string AppName { get; } = "FMU-API";
        public int AppVersion { get; set; } = 9;
        public int Assembly { get; set; } = 10;
        public string NodeName { get; set; } = string.Empty;
        public ServerConfig ServerConfig { get; set; } = new();
        public List<StringValue> HostsToPing { get; set; } = [];
        public MinimalPrices MinimalPrices { get; set; } = new();
        public OrganisationConfigurution OrganisationConfig { get; set; } = new();
        public AlcoUnitConfig FrontolAlcoUnit { get; set; } = new();
        public CouchDbConnection Database { get; set; } = new();
        public TokenService TrueSignTokenService { get; set; } = new();
        public HttpRequestTimeouts HttpRequestTimeouts { get; set; } = new();
        public LogSettings Logging { get; set; } = new();
        public FrontolConnectionSettings FrontolConnectionSettings { get; set; } = new();
        public SaleControlConfig SaleControlConfig { get; set; } = new();
        public CentralServerConnectionSettings CentralServerConnectionSettings { get; set; } = new();
        [JsonInclude]
        public AutoUpdateOptions AutoUpdate { get; private set; } = AutoUpdateOptions.Create();

        // устаревшие параметры
        public string? HostToPing { get; set; }
        public string? XAPIKEY { get; set; }
        public List<TrueSignCdn>? Cdn { get; set; }
        public SignData? SignData { get; set; }

        private string _dataFolder = string.Empty;

        [JsonConstructor]
        public Parametrs()
        {

        }

        public void Init(string dataFolder)
        {
            _dataFolder = dataFolder;

            LoadFromJson();
        }

        private void LoadFromJson()
        {
            string configFileName = Path.Combine(_dataFolder,"config.json");

            if (!Directory.Exists(_dataFolder))
                Directory.CreateDirectory(_dataFolder);

            Parametrs loadedConstants = new();

            if (File.Exists(configFileName))
                loadedConstants = LoadFromFile(configFileName);
            else
                Save(loadedConstants, configFileName);

            if (loadedConstants == null)
                return;

            if (loadedConstants.AppVersion != AppVersion)
            {
                if (loadedConstants.AppVersion < 9)
                {
                    loadedConstants.HostsToPing = FillHostsToPing(loadedConstants.HostToPing);
                    loadedConstants.OrganisationConfig = MoveXapiConfiguration(loadedConstants.XAPIKEY);
                }

                loadedConstants.AppVersion = AppVersion;
                Save(loadedConstants, configFileName);
            }

            ServerConfig = loadedConstants.ServerConfig;
            FrontolAlcoUnit = loadedConstants.FrontolAlcoUnit;
            HostsToPing = loadedConstants.HostsToPing;
            Database = loadedConstants.Database;
            TrueSignTokenService = loadedConstants.TrueSignTokenService;
            HttpRequestTimeouts = loadedConstants.HttpRequestTimeouts;
            Logging = loadedConstants.Logging;
            MinimalPrices = loadedConstants.MinimalPrices;
            FrontolConnectionSettings = loadedConstants.FrontolConnectionSettings;
            OrganisationConfig = loadedConstants.OrganisationConfig;
            NodeName = loadedConstants.NodeName;
            SaleControlConfig = loadedConstants.SaleControlConfig;
            AutoUpdate = loadedConstants.AutoUpdate;
            CentralServerConnectionSettings = loadedConstants.CentralServerConnectionSettings;

            if (NodeName == string.Empty)
                NodeName = Environment.MachineName;

            Database.CheckDbNames();

            OrganisationConfig.FillIfEMpty();

            Save(this, configFileName);
        }

        private OrganisationConfigurution MoveXapiConfiguration(string? XAPIKEY)
        {
            if (XAPIKEY == null)
                return new();

            if (OrganisationConfig.PrintGroups.Count > 0)
                return OrganisationConfig;

            OrganisationConfigurution answer = new();

            PrintGroupData xapi = new()
            {
                Id = 1,
                XAPIKEY = XAPIKEY,
            };

            answer.PrintGroups.Add(xapi);

            return answer;
        }

        private static List<StringValue> FillHostsToPing(string? hostToPing)
        {
            if (hostToPing == null)
                return [];

            if (hostToPing == string.Empty)
                return [];

            var hosts = new List<string>();

            if (hostToPing == "https://mail.ru")
            {
                hosts.Add("mail.ru");
                hosts.Add("ya.ru");
                hosts.Add("au124.ru");
                hosts.Add("atol.ru");
                hosts.Add("google.com");
            }
            else
                hosts.Add(hostToPing);

            List<StringValue> answer = new();

            int i = 1;

            foreach (var host in hosts)
            {
                answer.Add(new()
                {
                    Id = i,
                    Value = host
                });

                i++;
            }

            return answer;
        }

        public void Save(Parametrs constantsToSave, string configFileName)
        {
            JsonSerializerOptions jsonOptions = GeneralJsonSerilizerOptions.Default();

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

        async public Task<bool> SaveAsync(Parametrs constantsToSave)
        {
            return await SaveAsync(constantsToSave, _dataFolder);
        }

        async public Task<bool> SaveAsync(Parametrs constantsToSave, string dataFolder)
        {
            string configFileName = Path.Combine(dataFolder, "config.json");

            JsonSerializerOptions jsonOptions = GeneralJsonSerilizerOptions.Default();

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

            configJson ??= "";

            if (configJson != "")
                constant = JsonSerializer.Deserialize<Parametrs>(configJson, GeneralJsonSerilizerOptions.Default());

            if (constant == null)
                return new Parametrs();

            return constant;
        }
        public object Clone() => MemberwiseClone();
    }
}
