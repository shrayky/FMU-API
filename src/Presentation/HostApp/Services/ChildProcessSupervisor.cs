using System.Diagnostics;
using HostApp.Models;

namespace HostApp.Services;

internal sealed class ChildProcessSupervisor(ILogger<ChildProcessSupervisor> logger) : IAsyncDisposable
{
    private readonly Dictionary<string, ManagedChild> _children = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();

    public IReadOnlySet<string> RunningVersionDirectories
    {
        get
        {
            lock (_sync)
            {
                return _children.Values
                    .Where(c => c.Process is { HasExited: false })
                    .Select(c => c.VersionDirectory)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
        }
    }

    public void EnsureStarted(ProductInfo product)
    {
        var latest = product.Latest;
        if (latest is null)
        {
            logger.LogWarning("У продукта {Product} нет версий для запуска", product.Name);
            return;
        }

        lock (_sync)
        {
            if (_children.TryGetValue(product.Name, out var existing) &&
                existing.Process is { HasExited: false })
            {
                if (string.Equals(existing.ExecutablePath, latest.ExecutablePath, StringComparison.OrdinalIgnoreCase))
                    return;

                logger.LogInformation(
                    "Продукт {Product}: смена версии {Old} => {New}",
                    product.Name,
                    existing.Version,
                    latest.Version);

                StopProcess(existing);
                _children.Remove(product.Name);
            }

            StartAndTrack(product.Name, latest, restartAttempt: 0);
        }
    }

    public void Supervise(IReadOnlyList<ProductInfo> products)
    {
        foreach (var product in products)
        {
            var latest = product.Latest;
            if (latest is null)
                continue;

            ManagedChild? child;
            lock (_sync)
            {
                _children.TryGetValue(product.Name, out child);
            }

            // Нет записи — первый запуск
            if (child is null)
            {
                EnsureStarted(product);
                continue;
            }

            // Жив, но появилась более новая версия
            if (child.Process is { HasExited: false })
            {
                if (!string.Equals(child.ExecutablePath, latest.ExecutablePath, StringComparison.OrdinalIgnoreCase))
                    EnsureStarted(product);
                continue;
            }

            // Упал — backoff, затем рестарт
            var now = DateTime.UtcNow;

            lock (_sync)
            {
                if (!_children.TryGetValue(product.Name, out child))
                    continue;

                if (child.Process is { HasExited: false })
                    continue;

                if (child.NextRestartUtc is null)
                {
                    var attempt = child.RestartAttempt + 1;
                    var delay = RestartDelay(attempt);

                    logger.LogWarning(
                        "Процесс {Product} завершился с кодом {ExitCode}. Следующий запуск через {Delay} (попытка {Attempt})",
                        product.Name,
                        child.Process?.ExitCode,
                        delay,
                        attempt);

                    _children[product.Name] = child with
                    {
                        RestartAttempt = attempt,
                        NextRestartUtc = now.Add(delay)
                    };
                    continue;
                }

                if (now < child.NextRestartUtc)
                    continue;

                logger.LogInformation(
                    "Перезапуск продукта {Product} (попытка {Attempt})",
                    product.Name,
                    child.RestartAttempt);

                child.Process?.Dispose();
                StartAndTrack(product.Name, latest, child.RestartAttempt);
            }
        }
    }

    public async Task StopAllAsync(TimeSpan gracefulTimeout)
    {
        List<ManagedChild> snapshot;
        lock (_sync)
        {
            snapshot = _children.Values.ToList();
            _children.Clear();
        }

        foreach (var child in snapshot)
            await StopProcessGracefullyAsync(child, gracefulTimeout);
    }

    private void StartAndTrack(string productName, ProductVersionInfo version, int restartAttempt)
    {
        var process = StartProcess(productName, version);
        if (process is null)
        {
            var delay = RestartDelay(restartAttempt + 1);
            _children[productName] = new ManagedChild(
                productName,
                version.Version,
                version.DirectoryPath,
                version.ExecutablePath,
                Process: null,
                RestartAttempt: restartAttempt + 1,
                NextRestartUtc: DateTime.UtcNow.Add(delay));
            return;
        }

        _children[productName] = new ManagedChild(
            productName,
            version.Version,
            version.DirectoryPath,
            version.ExecutablePath,
            process,
            RestartAttempt: 0,
            NextRestartUtc: null);
    }

    private Process? StartProcess(string productName, ProductVersionInfo version)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = version.ExecutablePath,
                WorkingDirectory = version.DirectoryPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = "--service",
            };

            var process = Process.Start(startInfo);
            if (process is null)
            {
                logger.LogError("Process.Start вернул null для {Product}", productName);
                return null;
            }

            logger.LogInformation(
                "Запущен {Product} v{Version}, PID={Pid}, path={Path}",
                productName,
                version.Version,
                process.Id,
                version.ExecutablePath);

            return process;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось запустить {Product} из {Path}", productName, version.ExecutablePath);
            return null;
        }
    }

    private void StopProcess(ManagedChild child)
    {
        if (child.Process is null || child.Process.HasExited)
        {
            child.Process?.Dispose();
            return;
        }

        try
        {
            logger.LogInformation("Остановка {Product} PID={Pid}", child.ProductName, child.Process.Id);
            child.Process.Kill(entireProcessTree: true);
            child.Process.WaitForExit(5_000);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ошибка при остановке {Product}", child.ProductName);
        }
        finally
        {
            child.Process.Dispose();
        }
    }

    private async Task StopProcessGracefullyAsync(ManagedChild child, TimeSpan gracefulTimeout)
    {
        if (child.Process is null || child.Process.HasExited)
        {
            child.Process?.Dispose();
            return;
        }

        try
        {
            logger.LogInformation("Остановка {Product} PID={Pid}", child.ProductName, child.Process.Id);
            child.Process.Kill(entireProcessTree: true);
            using var cts = new CancellationTokenSource(gracefulTimeout);
            try
            {
                await child.Process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Таймаут ожидания выхода {Product}", child.ProductName);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ошибка при остановке {Product}", child.ProductName);
        }
        finally
        {
            child.Process.Dispose();
        }
    }

    private static TimeSpan RestartDelay(int attempt) =>
        attempt switch
        {
            <= 1 => TimeSpan.FromSeconds(5),
            2 => TimeSpan.FromSeconds(30),
            _ => TimeSpan.FromSeconds(60)
        };

    public async ValueTask DisposeAsync() => await StopAllAsync(TimeSpan.FromSeconds(5));

    private sealed record ManagedChild(
        string ProductName,
        Version Version,
        string VersionDirectory,
        string ExecutablePath,
        Process? Process,
        int RestartAttempt,
        DateTime? NextRestartUtc);
}
