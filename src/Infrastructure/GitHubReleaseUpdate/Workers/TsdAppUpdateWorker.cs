using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.SoftwareUpdate;
using GitHubReleaseUpdate.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GitHubReleaseUpdate.Workers;

/// Скачивает приложение FmuApiTSD из последнего релиза GitHub в каталог ноды.
public class TsdAppUpdateWorker : BackgroundService
{
#if DEBUG
    private const int StartDelayMinutes = 1;
#else
    private const int StartDelayMinutes = 5;
#endif

    private readonly ILogger<TsdAppUpdateWorker> _logger;
    private readonly IParametersService _parametersService;
    private readonly ClientPackageCache _cache;

    private DateTime _nextCheckTime;

    public TsdAppUpdateWorker(
        ILogger<TsdAppUpdateWorker> logger,
        IParametersService parametersService,
        ClientPackageCache cache)
    {
        _logger = logger;
        _parametersService = parametersService;
        _cache = cache;

        _nextCheckTime = DateTime.Now.AddMinutes(StartDelayMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (DateTime.Now < _nextCheckTime)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ConfigureAwait(false);
                continue;
            }

            var configuration = await _parametersService.CurrentAsync().ConfigureAwait(false);
            var options = configuration.TsdAppUpdate;

            try
            {
                if (options.Enabled)
                {
                    var result = await _cache
                        .Refresh(ClientApplications.Tsd, options.ReleaseAddress(), stoppingToken)
                        .ConfigureAwait(false);
                    if (result.IsFailure)
                    {
                        _logger.LogError("Не удалось получить FmuApiTSD: {Error}", result.Error);
                    }
                    else if (result.Value.Errors.Count > 0)
                    {
                        _logger.LogError(
                            "FmuApiTSD получен частично, файлов {Downloaded}: {Errors}",
                            result.Value.Downloaded,
                            string.Join("; ", result.Value.Errors));
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Проверка обновления FmuApiTSD не удалась");
            }

            var intervalMinutes = Math.Max(1, options.CheckIntervalMinutes);
            _nextCheckTime = DateTime.Now.AddMinutes(intervalMinutes);

            _logger.LogInformation(
                "Проверка FmuApiTSD: следующая проверка запланирована на {NextCheckTime}",
                _nextCheckTime);
        }
    }
}
