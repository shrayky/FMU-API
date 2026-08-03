using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.GisMt.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TrueApiIntegration.Workers;

/// <summary>
/// Фоновая синхронизация входящих документов ГИС МТ.
/// </summary>
public class GisMtDocumentsSyncWorker : BackgroundService
{
    private readonly ILogger<GisMtDocumentsSyncWorker> _logger;
    private readonly IParametersService _parametersService;
    private readonly IServiceScopeFactory _scopeFactory;

    private const int StartDelayMinutes = 1;
    private const int DefaultIntervalMinutes = 10;

    public GisMtDocumentsSyncWorker(
        ILogger<GisMtDocumentsSyncWorker> logger,
        IParametersService parametersService,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _parametersService = parametersService;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(StartDelayMinutes), stoppingToken).ConfigureAwait(false);
        while (!stoppingToken.IsCancellationRequested)
        {
            var intervalMinutes = DefaultIntervalMinutes;

            try
            {
                var parameters = await _parametersService.CurrentAsync();
                intervalMinutes = parameters.GisMtSettings.MtDocumentsPollIntervalMinutes > 0
                    ? parameters.GisMtSettings.MtDocumentsPollIntervalMinutes
                    : DefaultIntervalMinutes;

                var enabledGroups = parameters.OrganisationConfig.PrintGroups
                    .Where(x => x.TrueApiIntegrationSettings.Enable && !string.IsNullOrWhiteSpace(x.INN))
                    .ToList();

                if (enabledGroups.Count > 0)
                {
                    _logger.LogInformation("Запуск синхронизации входящих документов ГИС МТ");

                    using var scope = _scopeFactory.CreateScope();
                    var syncService = scope.ServiceProvider.GetRequiredService<IGisMtDocumentsSyncService>();
                    var result = await syncService.Sync(stoppingToken);

                    if (result.IsFailure)
                    {
                        _logger.LogWarning("Синхронизация ГИС МТ завершилась ошибкой: {Error}", result.Error);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Синхронизация ГИС МТ: организаций={Orgs}, документов={Docs}, марок={Marks}, удалено={Deleted}, ошибок={Errors}",
                            result.Value.OrganisationsProcessed,
                            result.Value.DocumentsLoaded,
                            result.Value.MarksSaved,
                            result.Value.MarksDeleted,
                            result.Value.Errors.Count);

                        foreach (var error in result.Value.Errors)
                            _logger.LogWarning("ГИС МТ sync: {Error}", error);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Ошибка worker синхронизации документов ГИС МТ");
            }

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken).ConfigureAwait(false);
        }
    }
}
