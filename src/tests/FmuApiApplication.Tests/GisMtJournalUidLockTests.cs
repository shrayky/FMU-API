using FmuApiApplication.GisMt.Journal;
using Xunit;

namespace FmuApiApplication.Tests;

public class GisMtJournalUidLockTests
{
    [Fact]
    public async Task Отмена_ожидания_не_ломает_очередь()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await GisMtJournalUidLock.Acquire("uid-lock-cancelled", cancellation.Token));

        // После отменённого ожидания тот же ключ должен снова браться без SemaphoreFullException.
        using var gate = await GisMtJournalUidLock.Acquire("uid-lock-cancelled");

        Assert.NotNull(gate);
    }

    [Fact]
    public async Task Второй_вызов_ждёт_освобождения_первого()
    {
        using var first = await GisMtJournalUidLock.Acquire("uid-lock-queue");

        using var cancellation = new CancellationTokenSource();
        var waiting = GisMtJournalUidLock.Acquire("uid-lock-queue", cancellation.Token);

        Assert.False(waiting.IsCompleted);

        first.Dispose();

        using var second = await waiting;

        Assert.NotNull(second);
    }
}
