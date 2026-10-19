using FmuApiDomain.CentralServiceExchange.Models.Answer;
using FmuApiDomain.Configuration.Options.Organization;
using FmuApiDomain.TrueApiIntegration;
using Xunit;

namespace FmuApiApplication.Tests;

public class XApiKeysFromExchangeApplierTests
{
    [Fact]
    public void Apply_истекший_оставляет_ключ_и_ставит_признак()
    {
        var first = Group("246412218294", "local-key");
        var second = Group(" 246412218294 ", "local-key-2");
        second.Id = 2;

        var changed = XApiKeysFromExchangeApplier.Apply(
            [
                new XApiKeyFromCentral
                {
                    Inn = "246412218294",
                    XApiKey = "не-подменять",
                    XApiKeyExpired = true
                }
            ],
            [first, second]);

        Assert.True(changed);
        Assert.Equal("local-key", first.XAPIKEY);
        Assert.True(first.XApiKeyExpired);
        Assert.Equal("local-key-2", second.XAPIKEY);
        Assert.True(second.XApiKeyExpired);
    }

    [Fact]
    public void Apply_повторный_истекший_не_помечает_сохранение()
    {
        var group = Group("246412218294", "local-key");
        group.XApiKeyExpired = true;

        var changed = XApiKeysFromExchangeApplier.Apply(
            [new XApiKeyFromCentral { Inn = "246412218294", XApiKeyExpired = true }],
            [group]);

        Assert.False(changed);
        Assert.Equal("local-key", group.XAPIKEY);
        Assert.True(group.XApiKeyExpired);
    }

    [Fact]
    public void Apply_живой_ключ_записывается_и_признак_снимается()
    {
        var group = Group("246412218294", "local-key");
        group.XApiKeyExpired = true;

        var changed = XApiKeysFromExchangeApplier.Apply(
            [
                new XApiKeyFromCentral
                {
                    Inn = "246412218294",
                    XApiKey = "cdn-key",
                    XApiKeyExpired = false
                }
            ],
            [group]);

        Assert.True(changed);
        Assert.Equal("cdn-key", group.XAPIKEY);
        Assert.False(group.XApiKeyExpired);
    }

    [Fact]
    public void Apply_тот_же_живой_ключ_без_признака_не_сохраняет()
    {
        var group = Group("246412218294", "cdn-key");

        var changed = XApiKeysFromExchangeApplier.Apply(
            [new XApiKeyFromCentral { Inn = "246412218294", XApiKey = "cdn-key", XApiKeyExpired = false }],
            [group]);

        Assert.False(changed);
        Assert.Equal("cdn-key", group.XAPIKEY);
        Assert.False(group.XApiKeyExpired);
    }

    [Fact]
    public void Apply_пустой_живой_элемент_и_чужой_инн_не_трогают_ключ()
    {
        var group = Group("1111111111", "local-key");

        Assert.False(XApiKeysFromExchangeApplier.Apply(null, [group]));
        Assert.False(XApiKeysFromExchangeApplier.Apply([], [group]));
        Assert.False(XApiKeysFromExchangeApplier.Apply(
            [new XApiKeyFromCentral { Inn = "246412218294", XApiKey = " ", XApiKeyExpired = false }],
            [group]));
        Assert.False(XApiKeysFromExchangeApplier.Apply(
            [new XApiKeyFromCentral { Inn = "246412218294", XApiKey = "cdn-key", XApiKeyExpired = false }],
            [group]));

        Assert.Equal("local-key", group.XAPIKEY);
        Assert.False(group.XApiKeyExpired);
    }

    [Fact]
    public void Resolve_истекший_подставляет_токен_а_живой_ключ_оставляет()
    {
        var expired = new PrintGroupData { XAPIKEY = "local-key", XApiKeyExpired = true };
        var active = new PrintGroupData { XAPIKEY = "local-key" };

        var empty = new PrintGroupData { XAPIKEY = "  " };

        Assert.Equal("true-token", XApiKeyCredential.Resolve(expired, " true-token "));
        Assert.Equal(string.Empty, XApiKeyCredential.Resolve(expired, " "));
        Assert.Equal(string.Empty, XApiKeyCredential.Resolve(expired, null));
        Assert.Equal("local-key", XApiKeyCredential.Resolve(active, "true-token"));
        Assert.Equal("true-token", XApiKeyCredential.Resolve(empty, " true-token "));
        Assert.Equal(string.Empty, XApiKeyCredential.Resolve(empty, " "));
        Assert.Equal(string.Empty, XApiKeyCredential.Resolve(null, "true-token"));
    }

    [Fact]
    public void PrintGroupByInn_сравнивает_после_trim()
    {
        var config = new OrganizationConfiguration
        {
            PrintGroups =
            [
                new PrintGroupData { Id = 1, INN = " 246412218294 " },
                new PrintGroupData { Id = 2, INN = "7700000000" }
            ]
        };

        Assert.Equal(1, config.PrintGroupByInn("246412218294")!.Id);
        Assert.Null(config.PrintGroupByInn(" "));
        Assert.Null(config.PrintGroupByInn("000"));
    }

    private static PrintGroupData Group(string inn, string key)
        => new()
        {
            Id = 1,
            INN = inn,
            XAPIKEY = key
        };
}
