﻿using FmuApiDomain.Authentication.Models;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Configuration.Options.Organization;
using System.Text.Json.Serialization;

namespace FmuApiDomain.Configuration
{
    public class Parameters
    {
        public string AppName { get; } = "FMU-API";
        public int AppVersion { get; set; } = 9;
        public int Assembly { get; set; } = 10;
        public string NodeName { get; set; } = string.Empty;
        public ServerConfig ServerConfig { get; set; } = new();
        public List<StringValue> HostsToPing { get; set; } = [];
        public MinimalPrices MinimalPrices { get; set; } = new();
        public OrganizationConfiguration OrganisationConfig { get; set; } = new();
        public AlcoUnitConfig FrontolAlcoUnit { get; set; } = new();
        public CouchDbConnection Database { get; set; } = new();
        public TokenServiceConfiguration TrueSignTokenService { get; set; } = new();
        public HttpRequestTimeouts HttpRequestTimeouts { get; set; } = new();
        public LogSettings Logging { get; set; } = new();
        public FrontolConnectionSettings FrontolConnectionSettings { get; set; } = new();
        public SaleControlConfig SaleControlConfig { get; set; } = new();
        public CentralServerConnectionProperties FmuApiCentralServer { get; set; } = new();
        [JsonInclude]
        public AutoUpdateOptions AutoUpdate { get; private set; } = AutoUpdateOptions.Create();

        // устаревшие параметры
        public string? HostToPing { get; set; }
        public string? XAPIKEY { get; set; }
        //public List<TrueSignCdn>? Cdn { get; set; }
        public TokenData? SignData { get; set; }

        private string _dataFolder = string.Empty;

        [JsonConstructor]
        public Parameters()
        {

        }

    }
}
