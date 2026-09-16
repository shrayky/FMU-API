using FmuApiApplication.State;
using FmuApiDomain.CentralServiceExchange.Models.Answer;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Configuration.Options.Organization;
using FmuApiDomain.TrueApiIntegration;
using Xunit;

namespace FmuApiApplication.Tests;

public class TrueApiTokensFromExchangeApplierTests
{
    [Fact]
    public void Apply_пишет_токен_в_состояние_по_инн_организации()
    {
        var state = new ApplicationState();
        var expired = DateTime.Now.AddHours(8);
        var organisations = new List<PrintGroupData>
        {
            Organisation("246412218294", useExternalToken: true)
        };
        var gisMt = new GisMtSettings { StockLoadEnabled = true };

        TrueApiTokensFromExchangeApplier.Apply(
            [new TrueApiTokenFromCentral { Inn = "246412218294", Token = "token-1", Expired = expired }],
            organisations,
            gisMt,
            state);

        var stored = state.TrueApiToken("246412218294");
        Assert.Equal("token-1", stored.Token);
        Assert.Equal(expired, stored.ExpirationDate());
    }

    [Fact]
    public void Apply_не_пишет_токен_и_не_меняет_настройки_если_инн_не_найден()
    {
        var state = new ApplicationState();
        var org = Organisation("1111111111", enable: false, useExternalToken: false);
        var gisMt = new GisMtSettings { StockLoadEnabled = false };

        var changed = TrueApiTokensFromExchangeApplier.Apply(
            [new TrueApiTokenFromCentral { Inn = "246412218294", Token = "token-1", Expired = DateTime.Now.AddHours(1) }],
            [org],
            gisMt,
            state);

        Assert.False(changed);
        Assert.False(org.TrueApiIntegrationSettings.Enable);
        Assert.False(org.TrueApiIntegrationSettings.UseExternalToken);
        Assert.False(gisMt.StockLoadEnabled);
        Assert.Equal(string.Empty, state.TrueApiToken("246412218294").Token);
        Assert.Equal(string.Empty, state.TrueApiToken("1111111111").Token);
    }

    [Fact]
    public void Apply_включает_внешний_токен_если_организация_получала_его_через_криптопро()
    {
        var state = new ApplicationState();
        var org = Organisation("246412218294", useExternalToken: false);
        var gisMt = new GisMtSettings();

        var changed = TrueApiTokensFromExchangeApplier.Apply(
            [new TrueApiTokenFromCentral { Inn = "246412218294", Token = "token-1", Expired = DateTime.Now.AddHours(1) }],
            [org],
            gisMt,
            state);

        Assert.True(changed);
        Assert.True(org.TrueApiIntegrationSettings.Enable);
        Assert.True(org.TrueApiIntegrationSettings.UseExternalToken);
        Assert.Equal("token-1", state.TrueApiToken("246412218294").Token);
    }

    [Fact]
    public void Apply_включает_интеграцию_если_она_выключена()
    {
        var state = new ApplicationState();
        var org = Organisation("246412218294", enable: false, useExternalToken: true);
        var gisMt = new GisMtSettings();

        var changed = TrueApiTokensFromExchangeApplier.Apply(
            [new TrueApiTokenFromCentral { Inn = "246412218294", Token = "token-1", Expired = DateTime.Now.AddHours(1) }],
            [org],
            gisMt,
            state);

        Assert.True(changed);
        Assert.True(org.TrueApiIntegrationSettings.Enable);
        Assert.True(org.TrueApiIntegrationSettings.UseExternalToken);
        Assert.Equal("token-1", state.TrueApiToken("246412218294").Token);
    }

    [Fact]
    public void Apply_включает_загрузку_остатков_гис_мт()
    {
        var state = new ApplicationState();
        var org = Organisation("246412218294", enable: false, useExternalToken: false);
        var gisMt = new GisMtSettings { StockLoadEnabled = false };

        var changed = TrueApiTokensFromExchangeApplier.Apply(
            [new TrueApiTokenFromCentral { Inn = "246412218294", Token = "token-1", Expired = DateTime.Now.AddHours(1) }],
            [org],
            gisMt,
            state);

        Assert.True(changed);
        Assert.True(gisMt.StockLoadEnabled);
    }

    [Fact]
    public void Apply_возвращает_false_если_флаги_уже_включены()
    {
        var state = new ApplicationState();
        var org = Organisation("246412218294", useExternalToken: true);
        var gisMt = new GisMtSettings { StockLoadEnabled = true };

        var changed = TrueApiTokensFromExchangeApplier.Apply(
            [new TrueApiTokenFromCentral { Inn = "246412218294", Token = "token-1", Expired = DateTime.Now.AddHours(1) }],
            [org],
            gisMt,
            state);

        Assert.False(changed);
        Assert.Equal("token-1", state.TrueApiToken("246412218294").Token);
    }

    [Fact]
    public void Apply_сопоставляет_инн_без_учёта_пробелов()
    {
        var state = new ApplicationState();
        var expired = DateTime.Now.AddHours(2);
        var organisations = new List<PrintGroupData>
        {
            Organisation(" 246412218294 ", useExternalToken: true)
        };
        var gisMt = new GisMtSettings { StockLoadEnabled = true };

        TrueApiTokensFromExchangeApplier.Apply(
            [new TrueApiTokenFromCentral { Inn = "246412218294", Token = "token-trim", Expired = expired }],
            organisations,
            gisMt,
            state);

        Assert.Equal("token-trim", state.TrueApiToken("246412218294").Token);
    }

    private static PrintGroupData Organisation(string inn, bool useExternalToken, bool enable = true)
    {
        return new PrintGroupData
        {
            Id = 1,
            INN = inn,
            Name = "Тест",
            TrueApiIntegrationSettings = new TrueApiIntegrationSettings
            {
                Enable = enable,
                UseExternalToken = useExternalToken
            }
        };
    }
}
