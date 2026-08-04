using HostApp.Services;

namespace HostApp.Workers;

internal sealed class ProductsHostWorker(
    ProductDiscovery discovery,
    ChildProcessSupervisor supervisor,
    VersionCleanup cleanup,
    ILogger<ProductsHostWorker> logger) : BackgroundService
{
    private static readonly TimeSpan LoopInterval = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Host запущен. Каталог установки: {Root}", HostPaths.InstallRoot);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    Tick();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка цикла supervision");
                }

                await Task.Delay(LoopInterval, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // штатная остановка
        }
        finally
        {
            logger.LogInformation("Остановка дочерних процессов...");
            await supervisor.StopAllAsync(TimeSpan.FromSeconds(10));
            logger.LogInformation("Host остановлен");
        }
    }

    private void Tick()
    {
        var products = discovery.Discover(HostPaths.InstallRoot);
        if (products.Count == 0)
        {
            logger.LogWarning("Продукты не найдены в {Root}", HostPaths.InstallRoot);
            return;
        }

        supervisor.Supervise(products);

        var protectedPaths = supervisor.RunningVersionDirectories;
        foreach (var product in products)
            cleanup.Cleanup(product, protectedPaths, HostConstants.VersionsToKeep);
    }
}
