using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.GisMt.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TrueApiIntegration.Workers;

/// <summary>
/// Фоновый опрос статусов документов журнала ГИС МТ.
/// </summary>
public class GisMtJournalStatusWorker(
    ILogger<GisMtJournalStatusWorker> logger,
    IParametersService parametersService,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private const int StartDelaySeconds = 20;
    private const int PollIntervalSeconds = 60;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(StartDelaySeconds), stoppingToken).ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var parameters = await parametersService.CurrentAsync();

                if (parameters.GisMtSettings.DocumentsEnabled)
                {
                    using var scope = scopeFactory.CreateScope();
                    var journalService = scope.ServiceProvider.GetRequiredService<IGisMtJournalService>();
                    var result = await journalService.CheckPendingCrptStatuses(stoppingToken);

                    if (result.IsFailure)
                        logger.LogWarning("Опрос статусов журнала ГИС МТ завершился ошибкой: {Error}", result.Error);
                    else if (result.Value > 0)
                        logger.LogInformation("Опрос статусов журнала ГИС МТ: проверено документов {Count}", result.Value);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Таймаут HttpClient приходит как OperationCanceledException:
                // опрос останавливать нельзя, иначе отказы Честного знака не вернут марки в остаток.
                logger.LogError(ex, "Ошибка worker опроса статусов журнала ГИС МТ");
            }

            await Task.Delay(TimeSpan.FromSeconds(PollIntervalSeconds), stoppingToken).ConfigureAwait(false);
        }
    }
}
