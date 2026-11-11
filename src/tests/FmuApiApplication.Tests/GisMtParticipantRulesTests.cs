using FmuApiDomain.GisMt.Journal;
using Xunit;

namespace FmuApiApplication.Tests;

public class GisMtParticipantRulesTests
{
    [Fact]
    public void Юридическое_лицо_передаёт_только_КПП()
    {
        var participant = GisMtParticipantRules.Resolve(" 2464122182 ", " 246401001 ", "fias-1").Value;

        Assert.False(participant.IsIndividual);
        Assert.Equal("2464122182", participant.Inn);
        Assert.Equal("246401001", participant.Kpp);
        Assert.Equal(string.Empty, participant.FiasId);
    }

    [Fact]
    public void ИП_передаёт_только_ФИАС()
    {
        var participant = GisMtParticipantRules.Resolve("246412218294", "246401001", " fias-1 ").Value;

        Assert.True(participant.IsIndividual);
        Assert.Equal("fias-1", participant.FiasId);
        Assert.Equal(string.Empty, participant.Kpp);
    }

    [Theory]
    [InlineData("")]
    [InlineData("246412218")]
    [InlineData("24641221829")]
    [InlineData("2464122182941")]
    public void Другая_длина_ИНН_отказ(string inn)
    {
        Assert.True(GisMtParticipantRules.Resolve(inn, "246401001", "fias-1").IsFailure);
    }

    [Fact]
    public void Юридическое_лицо_без_КПП_отказ()
    {
        var result = GisMtParticipantRules.Resolve("2464122182", " ", "fias-1");

        Assert.True(result.IsFailure);
        Assert.Contains("КПП", result.Error);
    }

    [Fact]
    public void ИП_без_ФИАС_отказ()
    {
        var result = GisMtParticipantRules.Resolve("246412218294", "246401001", "");

        Assert.True(result.IsFailure);
        Assert.Contains("ФИАС", result.Error);
    }

    [Fact]
    public void Без_МОД_юридическое_лицо_обходится_без_КПП()
    {
        var participant = GisMtParticipantRules.Resolve("2464122182", "", "", modRequired: false).Value;

        Assert.False(participant.IsIndividual);
        Assert.Equal(string.Empty, participant.Kpp);
        Assert.Equal(string.Empty, participant.FiasId);
    }

    [Fact]
    public void Без_МОД_ИП_обходится_без_ФИАС()
    {
        var participant = GisMtParticipantRules.Resolve("246412218294", "246401001", "", modRequired: false).Value;

        Assert.True(participant.IsIndividual);
        Assert.Equal(string.Empty, participant.Kpp);
        Assert.Equal(string.Empty, participant.FiasId);
    }

    [Fact]
    public void Без_МОД_длина_ИНН_всё_равно_проверяется()
    {
        Assert.True(GisMtParticipantRules.Resolve("246412218", "", "", modRequired: false).IsFailure);
    }
}
