using FmuApiDomain.GisMt.Journal;
using Xunit;

namespace FmuApiApplication.Tests;

public class GisMtModRequirementTests
{
    [Theory]
    [InlineData(GisMtJournalKinds.LkReceipt, GisMtWriteOffReasons.Packing, true)]
    [InlineData(GisMtJournalKinds.LkReceipt, "RETAIL", true)]
    [InlineData(GisMtJournalKinds.LkReceipt, GisMtWriteOffReasons.Expiration, false)]
    [InlineData(GisMtJournalKinds.LkReceipt, "UTILIZATION", false)]
    [InlineData(GisMtJournalKinds.LkReceipt, "DESTRUCTION", false)]
    [InlineData(GisMtJournalKinds.ConnectTap, GisMtWriteOffReasons.Packing, true)]
    public void МОД_требуется_по_справочнику_причин(string kind, string reason, bool expected)
    {
        Assert.Equal(expected, GisMtModRequirement.Required(kind, reason));
    }

    [Fact]
    public void Пустая_причина_считается_фасовкой_и_требует_МОД()
    {
        Assert.True(GisMtModRequirement.Required(GisMtJournalKinds.LkReceipt, null));
    }
}
