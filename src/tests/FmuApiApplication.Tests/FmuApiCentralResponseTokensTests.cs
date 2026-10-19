using System.Text.Json;
using FmuApiDomain.CentralServiceExchange.Models.Answer;
using Xunit;

namespace FmuApiApplication.Tests;

public class FmuApiCentralResponseTokensTests
{
    [Fact]
    public void Deserialize_читает_токены_из_пакета_обмена()
    {
        const string json = """
            {
              "success": true,
              "trueApiTokens": [
                { "inn": "246412218294", "token": "abc", "expired": "2026-09-12T18:30:00" }
              ]
            }
            """;

        var response = JsonSerializer.Deserialize<FmuApiCentralResponse>(json);

        Assert.NotNull(response);
        var token = Assert.Single(response.TrueApiTokens);
        Assert.Equal("246412218294", token.Inn);
        Assert.Equal("abc", token.Token);
        Assert.Equal(new DateTime(2026, 9, 12, 18, 30, 0), token.Expired);
    }

    [Fact]
    public void Deserialize_читает_живой_и_истекший_x_api_key()
    {
        const string json = """
            {
              "success": true,
              "xApiKeys": [
                {
                  "inn": "246412218294",
                  "xApiKey": "cdn-key",
                  "xApiKeyExpired": false,
                  "expiresAt": "2027-01-01T00:00:00"
                },
                {
                  "inn": "7700000000",
                  "xApiKey": "",
                  "xApiKeyExpired": true,
                  "expiresAt": "2026-10-14T09:00:00"
                }
              ]
            }
            """;

        var response = JsonSerializer.Deserialize<FmuApiCentralResponse>(json);

        Assert.NotNull(response);
        Assert.Equal(2, response.XApiKeys.Count);
        Assert.Equal("cdn-key", response.XApiKeys[0].XApiKey);
        Assert.False(response.XApiKeys[0].XApiKeyExpired);
        Assert.Equal(new DateTime(2027, 1, 1, 0, 0, 0), response.XApiKeys[0].ExpiresAt);
        Assert.True(response.XApiKeys[1].XApiKeyExpired);
        Assert.Equal(string.Empty, response.XApiKeys[1].XApiKey);
    }

    [Fact]
    public void Deserialize_без_xApiKeys_пустой_список()
    {
        const string json = """
            { "success": true, "trueApiTokens": [] }
            """;

        var response = JsonSerializer.Deserialize<FmuApiCentralResponse>(json);

        Assert.NotNull(response);
        Assert.Empty(response.XApiKeys);
    }
}
