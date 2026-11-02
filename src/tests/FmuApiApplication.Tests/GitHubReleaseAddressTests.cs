using FmuApiDomain.SoftwareUpdate;
using Xunit;

namespace FmuApiApplication.Tests;

public class GitHubReleaseAddressTests
{
    [Theory]
    [InlineData("https://github.com/shrayky/fmu-api-tsd")]
    [InlineData("https://github.com/shrayky/fmu-api-tsd/")]
    [InlineData("https://github.com/shrayky/fmu-api-tsd/releases/latest")]
    [InlineData("https://api.github.com/repos/shrayky/fmu-api-tsd/releases/latest")]
    [InlineData("shrayky/fmu-api-tsd")]
    public void Адрес_репозитория_становится_запросом_последнего_релиза(string address)
    {
        var parsed = GitHubReleaseAddress.TryMetadataUrl(address, out var metadataUrl);

        Assert.True(parsed);
        Assert.Equal("https://api.github.com/repos/shrayky/fmu-api-tsd/releases/latest", metadataUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("https://github.com/shrayky")]
    [InlineData("https://example.com/shrayky/fmu-api-tsd")]
    public void Чужой_адрес_не_принимается(string address)
    {
        Assert.False(GitHubReleaseAddress.TryMetadataUrl(address, out _));
    }
}
