using FmuApiDomain.Configuration.Options;
using Xunit;

namespace FmuApiApplication.Tests;

public class TrueApiIntegrationSettingsTests
{
    [Fact]
    public void ShouldRequestTokenViaCryptoPro_true_если_включено_и_не_внешний_токен()
    {
        var settings = new TrueApiIntegrationSettings { Enable = true, UseExternalToken = false };

        Assert.True(settings.ShouldRequestTokenViaCryptoPro());
    }

    [Fact]
    public void ShouldRequestTokenViaCryptoPro_false_если_внешний_токен()
    {
        var settings = new TrueApiIntegrationSettings { Enable = true, UseExternalToken = true };

        Assert.False(settings.ShouldRequestTokenViaCryptoPro());
    }

    [Fact]
    public void ShouldRequestTokenViaCryptoPro_false_если_интеграция_выключена()
    {
        var settings = new TrueApiIntegrationSettings { Enable = false, UseExternalToken = false };

        Assert.False(settings.ShouldRequestTokenViaCryptoPro());
    }
}
