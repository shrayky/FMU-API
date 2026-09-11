using FmuApiApplication.State;
using FmuApiApplication.TrueApi;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Configuration.Options.Organization;
using Xunit;

namespace FmuApiApplication.Tests;

public class TrueApiTokenStateCollectorTests
{
    [Fact]
    public void Collect_не_загружен_если_токена_нет()
    {
        var settings = SettingsWithOrg("246412218294", "Большакова", enable: true);
        var state = new ApplicationState();

        var result = TrueApiTokenStateCollector.Collect(settings, state);

        var item = Assert.Single(result);
        Assert.Equal(1, item.Id);
        Assert.Equal("Большакова", item.Name);
        Assert.Equal("246412218294", item.Inn);
        Assert.False(item.Loaded);
        Assert.Null(item.Expired);
        Assert.Equal("Не загружен", item.Status);
    }

    [Fact]
    public void Collect_загружен_с_датой_если_токен_действует()
    {
        var settings = SettingsWithOrg("246412218294", "Большакова", enable: true);
        var state = new ApplicationState();
        var expired = new DateTime(2026, 9, 12, 18, 30, 0);
        state.UpdateTrueApiToken("246412218294", "token-1", expired);

        var result = TrueApiTokenStateCollector.Collect(settings, state);

        var item = Assert.Single(result);
        Assert.True(item.Loaded);
        Assert.Equal(expired, item.Expired);
        Assert.Equal("действует до 12.09.2026", item.Status);
    }

    [Fact]
    public void Collect_не_включает_организации_без_true_api()
    {
        var settings = SettingsWithOrg("246412218294", "Большакова", enable: false);
        var state = new ApplicationState();

        var result = TrueApiTokenStateCollector.Collect(settings, state);

        Assert.Empty(result);
    }

    private static Parameters SettingsWithOrg(string inn, string name, bool enable)
    {
        return new Parameters
        {
            OrganisationConfig = new OrganizationConfiguration
            {
                PrintGroups =
                [
                    new PrintGroupData
                    {
                        Id = 1,
                        INN = inn,
                        Name = name,
                        TrueApiIntegrationSettings = new TrueApiIntegrationSettings
                        {
                            Enable = enable
                        }
                    }
                ]
            }
        };
    }
}
