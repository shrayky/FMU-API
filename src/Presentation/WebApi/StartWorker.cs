namespace WebApi
{
    public class StartWorker : BackgroundService
    {
        private readonly ILogger<StartWorker> _logger;

        public StartWorker(ILogger<StartWorker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogWarning("Служба запущена");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
