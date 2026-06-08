using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FmuPacketTrapper.Worker;

public class ClearOldMarkFilesWorker : BackgroundService
{
    private readonly ILogger<ClearOldMarkFilesWorker> _logger;
    private readonly IParametersService _parametersService;

    private const int StartDelayMinutes = 5;
    private const int CheckIntervalMinutes = 10;
    private DateTime _nextWorkDate = DateTime.Now;

    public ClearOldMarkFilesWorker(ILogger<ClearOldMarkFilesWorker> logger, IParametersService parametersService)
    {
        _logger = logger;
        _parametersService = parametersService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
#if !DEBUG

        await Task.Delay(TimeSpan.FromMinutes(StartDelayMinutes), stoppingToken).ConfigureAwait(false);
#endif
        while (!stoppingToken.IsCancellationRequested)
        {
            if (DateTime.Now < _nextWorkDate)
            {
                await Task.Delay(TimeSpan.FromMinutes(CheckIntervalMinutes), stoppingToken).ConfigureAwait(false);
                continue;
            }

            var configuration = await _parametersService.CurrentAsync();

            if (configuration.SaleControlConfig.MarkCheckResultSave.Enable)
            {
                ClearFolder(configuration.SaleControlConfig.MarkCheckResultSave, stoppingToken);
            }

            _nextWorkDate = DateTime.Now.AddMinutes(CheckIntervalMinutes);
        }
    }

    private void ClearFolder(MarkCheckResultSave markCheckResultSave, CancellationToken stoppingToken)
    {
        var directory = markCheckResultSave.Directory;

        if (string.IsNullOrWhiteSpace(directory))
        {
            _logger.LogWarning("Не задан каталог для очистки файлов результатов проверки марки");
            return;
        }

        if (!Directory.Exists(directory))
            return;

        var cutoffDate = DateTime.Now.AddHours(-markCheckResultSave.FileLifespanHours);
        var deletedCount = 0;

        try
        {
            foreach (var filePath in Directory.EnumerateFiles(directory))
            {
                stoppingToken.ThrowIfCancellationRequested();

                var fileInfo = new FileInfo(filePath);
                if (fileInfo.LastWriteTime >= cutoffDate)
                    continue;

                File.Delete(filePath);
                deletedCount++;
            }

            if (deletedCount > 0)
            {
                _logger.LogInformation(
                    "Очистка каталога {Directory}: удалено {DeletedCount} файлов старше {LifespanHours} ч.",
                    directory,
                    deletedCount,
                    markCheckResultSave.FileLifespanHours);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Ошибка при очистке каталога {Directory} от устаревших файлов результатов проверки марки", directory);
        }
    }
}
