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
}
