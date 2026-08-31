using FmuApiApplication.Documents.Interfaces;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Documents.Workers;

/// <summary>
/// Периодически выгружает файловую очередь документов после восстановления CouchDB.
/// </summary>
public class OfflineDocumentFlushWorker(
    ILogger<OfflineDocumentFlushWorker> logger,
    IParametersService parametersService,
    IApplicationState applicationState,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_checkInterval, stoppingToken);

            var config = await parametersService.CurrentAsync();
            if (!config.Database.ConfigurationIsEnabled)
                continue;

            if (!applicationState.CouchDbOnline())
                continue;

            try
            {
                using var scope = scopeFactory.CreateScope();
                var flushService = scope.ServiceProvider.GetRequiredService<IOfflineDocumentFlushService>();
                await flushService.FlushAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка выгрузки файловой очереди документов");
            }
        }
    }
}
