using System.Text.Json.Nodes;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Configuration.Options.Organization;
using Xunit;

namespace FmuApiApplication.Tests;

public class SettingsExchangeTests
{
    [Fact]
    public void Export_берёт_только_помеченные_поля_и_не_берёт_криптопро()
    {
        var source = Sample();

        var json = SettingsExchange.Export(source);
        var root = JsonNode.Parse(json)!.AsObject();

        Assert.Equal(2599, root["ServerConfig"]!["ApiIpPort"]!.GetValue<int>());
        Assert.Equal("ping", root["HostsToPing"]![0]!["Value"]!.GetValue<string>());
        Assert.Equal(10, root["MinimalPrices"]!["Tabaco"]!.GetValue<int>());
        Assert.Equal("x-key", root["OrganisationConfig"]!["PrintGroups"]![0]!["XAPIKEY"]!.GetValue<string>());
        Assert.Equal("lm-secret", root["OrganisationConfig"]!["PrintGroups"]![0]!["LocalModuleConnection"]!["Password"]!.GetValue<string>());
        Assert.Equal("alco-secret", root["FrontolAlcoUnit"]!["Password"]!.GetValue<string>());
        Assert.Equal(7, root["Statistics"]!["DepthOfStorageOfStatisticsInDays"]!.GetValue<int>());
        Assert.Equal(4, root["HttpRequestTimeouts"]!["CheckMarkRequestTimeout"]!.GetValue<int>());
        Assert.Equal("Debug", root["Logging"]!["LogLevel"]!.GetValue<string>());
        Assert.True(root["SaleControlConfig"]!["UseBeerTaps"]!.GetValue<bool>());
        Assert.Equal(3, root["GisMtSettings"]!["DocumentsSyncDays"]!.GetValue<int>());
        Assert.Equal("Молоко", root["GisMtProductMappings"]![0]!["Name"]!.GetValue<string>());

        Assert.False(root.ContainsKey("Database"));
        Assert.False(root.ContainsKey("NodeName"));
        Assert.False(root.ContainsKey("FmuApiCentralServer"));
        Assert.False(json.Contains("TrueApiIntegrationSettings", StringComparison.Ordinal));
        Assert.False(json.Contains("crypto-secret", StringComparison.Ordinal));
    }

    [Fact]
    public void TryApply_переносит_помеченные_поля_и_оставляет_криптопро()
    {
        var current = Sample();
        current.ServerConfig.ApiIpPort = 2578;
        current.Database.Password = "db-secret";
        current.NodeName = "Касса-1";
        current.Logging.LogLevel = "Warning";
        current.OrganisationConfig.PrintGroups[0].TrueApiIntegrationSettings.Password = "keep-crypto";
        current.OrganisationConfig.PrintGroups[0].TrueApiIntegrationSettings.DigitalSignature = "keep-sign";

        var file = """
            {
              "serverConfig": { "apiIpPort": 2601 },
              "organisationConfig": {
                "printGroups": [
                  {
                    "id": 5,
                    "xapikey": "new-key",
                    "name": "Новая",
                    "trueApiIntegrationSettings": {
                      "password": "from-file",
                      "digitalSignature": "from-file"
                    }
                  }
                ]
              }
            }
            """;

        var applied = SettingsExchange.TryApply(current, file, out var changed, out var error);

        Assert.True(applied, error);
        Assert.True(changed);
        Assert.Equal(2601, current.ServerConfig.ApiIpPort);
        Assert.Equal("db-secret", current.Database.Password);
        Assert.Equal("Касса-1", current.NodeName);
        Assert.Equal("Warning", current.Logging.LogLevel);
        Assert.Equal("new-key", current.OrganisationConfig.PrintGroups[0].XAPIKEY);
        Assert.Equal("keep-crypto", current.OrganisationConfig.PrintGroups[0].TrueApiIntegrationSettings.Password);
        Assert.Equal("keep-sign", current.OrganisationConfig.PrintGroups[0].TrueApiIntegrationSettings.DigitalSignature);
    }

    [Fact]
    public void TryApply_новая_организация_не_получает_криптопро_из_файла()
    {
        var current = Sample();
        var file = """
            {
              "OrganisationConfig": {
                "PrintGroups": [
                  {
                    "Id": 9,
                    "XAPIKEY": "other-key",
                    "TrueApiIntegrationSettings": {
                      "Password": "from-file",
                      "DigitalSignature": "from-file"
                    }
                  }
                ]
              }
            }
            """;

        var applied = SettingsExchange.TryApply(current, file, out var changed, out var error);

        Assert.True(applied, error);
        Assert.True(changed);
        Assert.Equal("other-key", current.OrganisationConfig.PrintGroups[0].XAPIKEY);
        Assert.Equal(string.Empty, current.OrganisationConfig.PrintGroups[0].TrueApiIntegrationSettings.Password);
        Assert.Equal(string.Empty, current.OrganisationConfig.PrintGroups[0].TrueApiIntegrationSettings.DigitalSignature);
    }

    [Fact]
    public void TryApply_битый_json_не_меняет_настройки()
    {
        var current = Sample();

        var applied = SettingsExchange.TryApply(current, "{", out var changed, out var error);

        Assert.False(applied);
        Assert.False(changed);
        Assert.False(string.IsNullOrWhiteSpace(error));
        Assert.Equal(2599, current.ServerConfig.ApiIpPort);
    }

    private static Parameters Sample()
    {
        return new Parameters
        {
            NodeName = "Узел",
            ServerConfig = new ServerConfig { ApiIpPort = 2599 },
            HostsToPing = [new StringValue { Id = 1, Value = "ping" }],
            MinimalPrices = new MinimalPrices { Tabaco = 10 },
            OrganisationConfig = new OrganizationConfiguration
            {
                PrintGroups =
                [
                    new PrintGroupData
                    {
                        Id = 5,
                        XAPIKEY = "x-key",
                        Name = "Организация",
                        LocalModuleConnection = new LocalModuleConnection { Password = "lm-secret" },
                        TrueApiIntegrationSettings = new TrueApiIntegrationSettings
                        {
                            Password = "crypto-secret",
                            DigitalSignature = "sign"
                        }
                    }
                ]
            },
            FrontolAlcoUnit = new AlcoUnitConfig { Password = "alco-secret" },
            Database = new CouchDbConnection { Password = "db-secret" },
            Statistics = new StatisticsSettings { DepthOfStorageOfStatisticsInDays = 7 },
            HttpRequestTimeouts = new HttpRequestTimeouts { CheckMarkRequestTimeout = 4 },
            Logging = new LogSettings { LogLevel = "Debug" },
            SaleControlConfig = new SaleControlConfig { UseBeerTaps = true },
            GisMtSettings = new GisMtSettings { DocumentsSyncDays = 3 },
            GisMtProductMappings = [new GisMtProductMapping { Name = "Молоко" }],
            FmuApiCentralServer = new CentralServerConnectionProperties { Token = "central-token" }
        };
    }
}
