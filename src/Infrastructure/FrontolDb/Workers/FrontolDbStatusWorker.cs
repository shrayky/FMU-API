using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Frontol.Interfaces;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FrontolDb.Workers;

/// <summary>
/// Периодически проверяет доступность базы справочника товаров Frontol и пишет статус в состояние приложения.
/// </summary>
public class FrontolDbStatusWorker(
    ILogger<FrontolDbStatusWorker> logger,
    IParametersService parametersService,
    IApplicationState applicationState,
    IFrontolConnectionProbe connectionProbe) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckFrontolState(stoppingToken);

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    /// <summary>
    /// Проверяет выбранное подключение справочника товаров. Другие подключения, включая базу кранов, не трогает.
    /// </summary>
    private async Task CheckFrontolState(CancellationToken stoppingToken)
    {
        var configuration = await parametersService.CurrentAsync();
        var wareDataSourceId = configuration.ConnectedFrontolSettings.ResolveWareDataSourceId();
        var connection = configuration.ConnectedFrontolSettings.ConnectionSettings
            .FirstOrDefault(item => item.Id == wareDataSourceId);

        var isOnline = applicationState.FrontolDbOnline();

        if (connection == null || !connection.ConnectionEnable())
        {
            if (isOnline)
                logger.LogCritical("Изменение статуса базы Frontol, новый статус - не выбрана");

            applicationState.UpdateFrontolDbState(false);
            return;
        }

        var probeResult = await connectionProbe.Probe(connection, stoppingToken);
        var nowState = probeResult.IsSuccess;

        if (nowState == isOnline)
            return;

        if (probeResult.IsFailure)
            logger.LogCritical(
                "Изменение статуса базы Frontol {beforeCheck} -> {afterCheck}: {error}",
                isOnline,
                nowState,
                probeResult.Error);
        else
            logger.LogCritical("Изменение статуса базы Frontol {beforeCheck} -> {afterCheck}", isOnline, nowState);

        applicationState.UpdateFrontolDbState(nowState);
    }
}
