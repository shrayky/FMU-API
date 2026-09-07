using Shared.Json;
using Xunit;

namespace Shared.Tests;

public class JsonHelpersDeserializeTests
{
    private sealed class ProductMappingDto
    {
        public int AtolCode { get; set; }
        public int TrueApiGroupId { get; set; }
        public bool CheckSmp { get; set; }
        public bool CheckMrp { get; set; }
    }

    [Fact]
    public async Task DeserializeAsync_string_читает_camelCase_в_PascalCase_свойства()
    {
        const string json = """{"atolCode":31,"trueApiGroupId":3,"checkSmp":true,"checkMrp":false}""";

        var result = await JsonHelpers.DeserializeAsync<ProductMappingDto>(json);

        Assert.NotNull(result);
        Assert.Equal(31, result.AtolCode);
        Assert.Equal(3, result.TrueApiGroupId);
        Assert.True(result.CheckSmp);
        Assert.False(result.CheckMrp);
    }
}
