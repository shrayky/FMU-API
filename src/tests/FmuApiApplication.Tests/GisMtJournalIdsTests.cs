using FmuApiDomain.GisMt.Journal;
using Xunit;

namespace FmuApiApplication.Tests;

public class GisMtJournalIdsTests
{
    [Fact]
    public void Идентификатор_акта_из_uid_детерминированный()
    {
        var first = GisMtJournalIds.FromUid(" uid-1 ");
        var second = GisMtJournalIds.FromUid("uid-1");

        Assert.Equal(first, second);
        Assert.True(Guid.TryParse(first, out _));
    }

    [Fact]
    public void Разные_uid_дают_разные_идентификаторы()
    {
        Assert.NotEqual(GisMtJournalIds.FromUid("uid-1"), GisMtJournalIds.FromUid("uid-2"));
        Assert.NotEqual(GisMtJournalIds.FromUid(string.Empty), GisMtJournalIds.FromUid(string.Empty));
    }
}
