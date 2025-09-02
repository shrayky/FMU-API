using FmuApiDomain.Constants;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ServicesAndDaemonsManager.Workers
{
    public class ApplicationRestartWorker : BackgroundService
    {
        private readonly ILogger<ApplicationRestartWorker> _logger;
        private readonly IApplicationState _applicationState;

        private const int CheckIntervalMinutes = 1;

        public ApplicationRestartWorker(ILogger<ApplicationRestartWorker> logger, IApplicationState applicationState)
        {
            _logger = logger;
            _applicationState = applicationState;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(CheckIntervalMinutes), stoppingToken);

                if (!_applicationState.NeedRestartService())
                    continue;

                _logger.LogWarning("Будет произведен перезапуск приложения из-за изменения настроек.");

                var manager = ServiceAndDaemonsManagerFactory.Create();

                manager.Restart(ApplicationInformation.AppName.ToLower());
            }
        }
    }
}
