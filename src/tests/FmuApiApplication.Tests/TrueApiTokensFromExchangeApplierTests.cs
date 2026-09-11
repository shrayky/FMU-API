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

        TrueApiTokensFromExchangeApplier.Apply(
            [new TrueApiTokenFromCentral { Inn = "246412218294", Token = "token-1", Expired = expired }],
            organisations,
            state);

        var stored = state.TrueApiToken("246412218294");
        Assert.Equal("token-1", stored.Token);
        Assert.Equal(expired, stored.ExpirationDate());
    }

    [Fact]
    public void Apply_не_пишет_токен_если_инн_не_найден()
    {
        var state = new ApplicationState();
        var organisations = new List<PrintGroupData>
        {
            Organisation("1111111111", useExternalToken: true)
        };

        TrueApiTokensFromExchangeApplier.Apply(
            [new TrueApiTokenFromCentral { Inn = "246412218294", Token = "token-1", Expired = DateTime.Now.AddHours(1) }],
            organisations,
            state);

        Assert.Equal(string.Empty, state.TrueApiToken("246412218294").Token);
        Assert.Equal(string.Empty, state.TrueApiToken("1111111111").Token);
    }

    [Fact]
    public void Apply_не_пишет_токен_если_организация_получает_его_через_криптопро()
    {
        var state = new ApplicationState();
        var organisations = new List<PrintGroupData>
        {
            Organisation("246412218294", useExternalToken: false)
        };

        TrueApiTokensFromExchangeApplier.Apply(
            [new TrueApiTokenFromCentral { Inn = "246412218294", Token = "token-1", Expired = DateTime.Now.AddHours(1) }],
            organisations,
            state);

        Assert.Equal(string.Empty, state.TrueApiToken("246412218294").Token);
    }

    [Fact]
    public void Apply_не_пишет_токен_если_интеграция_выключена()
    {
        var state = new ApplicationState();
        var org = Organisation("246412218294", useExternalToken: true);
        org.TrueApiIntegrationSettings.Enable = false;

        TrueApiTokensFromExchangeApplier.Apply(
            [new TrueApiTokenFromCentral { Inn = "246412218294", Token = "token-1", Expired = DateTime.Now.AddHours(1) }],
            [org],
            state);

        Assert.Equal(string.Empty, state.TrueApiToken("246412218294").Token);
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

        TrueApiTokensFromExchangeApplier.Apply(
            [new TrueApiTokenFromCentral { Inn = "246412218294", Token = "token-trim", Expired = expired }],
            organisations,
            state);

        Assert.Equal("token-trim", state.TrueApiToken("246412218294").Token);
    }

    private static PrintGroupData Organisation(string inn, bool useExternalToken)
    {
        return new PrintGroupData
        {
            Id = 1,
            INN = inn,
            Name = "Тест",
            TrueApiIntegrationSettings = new TrueApiIntegrationSettings
            {
                Enable = true,
                UseExternalToken = useExternalToken
            }
        };
    }
}
