using FmuApiDomain.Configuration.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Workers
{
    public class AutoConfigurationApply250901Worker : BackgroundService
    {
        private readonly IParametersService _parametersService;
        private readonly ILogger<AutoConfigurationApply250901Worker> _logger;

        private readonly DateTime _workData = new(2025, 9, 1);
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);
        private readonly TimeSpan _startDelay = TimeSpan.FromMinutes(1);

        public AutoConfigurationApply250901Worker(IParametersService parametersService, ILogger<AutoConfigurationApply250901Worker> logger)
        {
            _parametersService = parametersService;
            _logger = logger;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            
            await Task.Delay(_startDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {

                if (DateTime.Today > _workData)
                    break;

                if (DateTime.Today != _workData)
                {
                    await Task.Delay(_checkInterval, stoppingToken);
                    continue;
                }

                var appSettings = await _parametersService.CurrentAsync();

                if (appSettings.SaleControlConfig.SendLocalModuleInformationalInRequestId)
                    break;

                appSettings.SaleControlConfig.SendLocalModuleInformationalInRequestId = true;

                await _parametersService.UpdateAsync(appSettings);

                _logger.LogWarning("Автоматически установлен параметр SendLocalModuleInformationalInRequestId");

                break;
            }
        }
    }
}
