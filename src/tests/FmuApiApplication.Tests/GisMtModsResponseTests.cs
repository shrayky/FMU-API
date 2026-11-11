using FmuApiDomain.GisMt.Models;
using Xunit;

namespace FmuApiApplication.Tests;

public class GisMtModsResponseTests
{
    [Fact]
    public void Разбирается_объект_с_nextPage()
    {
        var response = GisMtModsResponse.Parse("""
            {
              "mods": [
                { "address": "г. Москва, ул. Ленина, 1", "fiasId": "fias-1", "kpp": "246401001" }
              ],
              "nextPage": true
            }
            """);

        Assert.True(response.NextPage);
        var place = Assert.Single(response.Mods);
        Assert.Equal("246401001", place.Kpp);
        Assert.Equal("fias-1", place.FiasId);
    }

    [Fact]
    public void Разбирается_голый_массив()
    {
        var response = GisMtModsResponse.Parse("""
            [ { "address": "адрес", "fiasId": "", "kpp": "" } ]
            """);

        Assert.False(response.NextPage);
        Assert.Single(response.Mods);
    }

    [Fact]
    public void Пустой_ответ_даёт_пустой_список()
    {
        Assert.Empty(GisMtModsResponse.Parse("").Mods);
    }
}
