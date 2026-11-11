namespace FmuApiApplication.GisMt.Journal;

/// <summary>
/// Очередь обработки операций Фронтола с одинаковым uid в пределах процесса:
/// два параллельных connect_tap не должны создавать два акта и отправлять два документа.
/// </summary>
internal static class GisMtJournalUidLock
{
    private sealed class Gate
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);

        public int Users { get; set; }
    }

    private sealed class Releaser(string key, bool acquired) : IDisposable
    {
        private int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
                Release(key, acquired);
        }
    }

    private static readonly object Sync = new();
    private static readonly Dictionary<string, Gate> Gates = new(StringComparer.Ordinal);

    public static async Task<IDisposable> Acquire(string uid, CancellationToken cancellationToken = default)
    {
        var key = (uid ?? string.Empty).Trim();

        if (key.Length == 0)
            return new Releaser(string.Empty, acquired: false);

        SemaphoreSlim semaphore;

        lock (Sync)
        {
            if (!Gates.TryGetValue(key, out var gate))
            {
                gate = new Gate();
                Gates[key] = gate;
            }

            gate.Users++;
            semaphore = gate.Semaphore;
        }

        try
        {
            await semaphore.WaitAsync(cancellationToken);
        }
        catch
        {
            Release(key, acquired: false);
            throw;
        }

        return new Releaser(key, acquired: true);
    }

    private static void Release(string key, bool acquired)
    {
        if (key.Length == 0)
            return;

        SemaphoreSlim? semaphore = null;

        lock (Sync)
        {
            if (!Gates.TryGetValue(key, out var gate))
                return;

            gate.Users--;

            if (gate.Users <= 0)
                Gates.Remove(key);

            // Отпускает семафор только тот, кто его занял: отменённое ожидание его не брало.
            if (acquired)
                semaphore = gate.Semaphore;
        }

        semaphore?.Release();
    }
}
