using FmuApiSettings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Workers
{
    public class ClearOldLogsWorker : BackgroundService
    {
        private readonly ILogger<ClearOldLogsWorker> _logger;

        private DateTime nextWorkDate = DateTime.Now;
        private int CheckPeriodHours = 12;
        private string logFolderPath  = string.Concat(Constants.DataFolderPath, "\\log\\");

        public ClearOldLogsWorker(ILogger<ClearOldLogsWorker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (nextWorkDate >= DateTime.Now)
                {
                    await Task.Delay(10_000, stoppingToken);
                    continue;
                }

                _logger.LogInformation("Запуск очистки старых лог-файлов");

                nextWorkDate = DateTime.Now.AddHours(12);

                var files = Directory.GetFiles(logFolderPath, "fmu-api*.log");

                var deleteDate = DateTime.Now.AddDays(Constants.Parametrs.Logging.LogDepth * -1);
                var deleteFiles = new List<string>();

                files.OrderDescending().ToList().ForEach(fName =>
                {
                    if (File.GetCreationTime(fName) < deleteDate)
                        deleteFiles.Add(fName);
                });

                foreach (var fileName in deleteFiles)
                {
                    try
                    {
                        File.Delete(fileName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Не удалось удалить лог-файл {fileName} ошибка {ex}", fileName, ex);
                    }
                }

            }
        }
    }
}
