using FmuApiDomain.GisMt;
using Xunit;

namespace FmuApiApplication.Tests;

public class GisMtCisUniquenessTests
{
    [Fact]
    public void Продолжение_кода_отбрасывается_в_пользу_короткого()
    {
        var result = GisMtCisUniqueness.Distinct(
        [
            "0104607022743036215+/S3Wr(ItfARxyz",
            "0104607022743036215+/S3Wr(ItfAR",
            "0104607022743036215+/S3Wr(ItfAR"
        ]);

        Assert.Equal(["0104607022743036215+/S3Wr(ItfAR"], result);
    }

    [Fact]
    public void Разные_коды_остаются()
    {
        var result = GisMtCisUniqueness.Distinct(
        [
            "0104607022743036215AAA",
            "0104607022743036215BBB"
        ]);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Пустые_строки_не_попадают()
    {
        var result = GisMtCisUniqueness.Distinct([" ", "", "0104607022743036215AAA"]);

        Assert.Equal(["0104607022743036215AAA"], result);
    }
}
