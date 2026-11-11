using FmuApiDomain.GisMt.Journal;
using Xunit;

namespace FmuApiApplication.Tests;

public class GisMtCisGtinTests
{
    [Fact]
    public void GTIN_берётся_из_начала_КИ()
    {
        Assert.Equal("04603334003509", GisMtCisGtin.Extract("0104603334003509215EQ26tdHwAvn"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("215EQ26tdHwAvn")]
    [InlineData("01046033340035")]
    public void Без_AI_01_или_короткая_КИ_дают_пустой_GTIN(string cis)
    {
        Assert.Equal(string.Empty, GisMtCisGtin.Extract(cis));
    }
}
