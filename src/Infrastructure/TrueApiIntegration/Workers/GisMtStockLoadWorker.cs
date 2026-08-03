using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.GisMt.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TrueApiIntegration.Workers;

/// <summary>
/// Ежедневная фоновая загрузка остатков марок ГИС МТ.
/// </summary>
public class GisMtStockLoadWorker : BackgroundService
{
    private readonly ILogger<GisMtStockLoadWorker> _logger;
    private readonly IParametersService _parametersService;
    private readonly IServiceScopeFactory _scopeFactory;

    private const int StartDelayMinutes = 1;
    private const int CheckIntervalMinutes = 1;

    private DateOnly? _lastRunDate;

    public GisMtStockLoadWorker(
        ILogger<GisMtStockLoadWorker> logger,
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
            try
            {
                var parameters = await _parametersService.CurrentAsync();
                var settings = parameters.GisMtSettings;

                if (settings.StockLoadEnabled && ShouldRunToday(settings.StockLoadTime))
                {
                    _logger.LogInformation("Запуск ежедневной загрузки остатков ГИС МТ");

                    using var scope = _scopeFactory.CreateScope();
                    var stockLoadService = scope.ServiceProvider.GetRequiredService<IGisMtStockLoadService>();
                    var result = await stockLoadService.LoadAll(stoppingToken);

                    _lastRunDate = DateOnly.FromDateTime(DateTime.Now);

                    if (result.IsFailure)
                    {
                        _logger.LogWarning("Загрузка остатков ГИС МТ завершилась ошибкой: {Error}", result.Error);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Загрузка остатков ГИС МТ: организаций={Orgs}, марок={Marks}, ошибок={Errors}",
                            result.Value.OrganisationsProcessed,
                            result.Value.MarksSaved,
                            result.Value.Errors.Count);

                        foreach (var error in result.Value.Errors)
                            _logger.LogWarning("ГИС МТ stock: {Error}", error);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Ошибка worker загрузки остатков ГИС МТ");
            }

            await Task.Delay(TimeSpan.FromMinutes(CheckIntervalMinutes), stoppingToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Проверяет, наступило ли время ежедневной загрузки и не выполнялась ли она сегодня.
    /// </summary>
    private bool ShouldRunToday(TimeOnly scheduledTime)
    {
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);

        if (_lastRunDate == today)
            return false;

        return TimeOnly.FromDateTime(now) >= scheduledTime;
    }
}
