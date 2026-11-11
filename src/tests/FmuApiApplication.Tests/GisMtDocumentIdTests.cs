using FmuApiDomain.GisMt.Journal;
using Xunit;

namespace FmuApiApplication.Tests;

public class GisMtDocumentIdTests
{
    [Fact]
    public void Идентификатор_читается_из_JSON_строки()
    {
        Assert.Equal("9abd3d41-76bc-4542-a88e-b1f7be8130b5",
            GisMtDocumentId.Parse("\"9abd3d41-76bc-4542-a88e-b1f7be8130b5\""));
    }

    [Fact]
    public void Идентификатор_читается_без_кавычек_и_пробелов()
    {
        Assert.Equal("9abd3d41", GisMtDocumentId.Parse("  9abd3d41  "));
    }

    [Fact]
    public void Пустой_ответ_даёт_пустой_идентификатор()
    {
        Assert.Equal(string.Empty, GisMtDocumentId.Parse(""));
        Assert.Equal(string.Empty, GisMtDocumentId.Parse(null));
    }
}
