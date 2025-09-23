using System.Net.Http.Headers;
using CentralServerExchange.Interfaces;
using CentralServerExchange.Services;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Node.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TrueApiCdn.Interface;

namespace CentralServerExchange.Workers
{
    public class CentralServerExchangeWorker : BackgroundService
    {
        private readonly ILogger<CentralServerExchangeWorker> _logger;
        
        private readonly IParametersService _parametersService;
        private readonly IExchangeService _exchangeService;
        private readonly INodeInformationService _nodeInformationService;
        
        private DateTime _nextExchangeTime;

        private const int TryAfterErrorLimit = 10;
        private const int StartDelayMinutes = 5;
        private const int DelayAfterErrorMinutes = 1;
        
        public CentralServerExchangeWorker(ILogger<CentralServerExchangeWorker> logger, IParametersService parametersService, INodeInformationService nodeInformationService, IExchangeService exchangeService)
        {
            _logger = logger;
            _parametersService = parametersService;
            _nodeInformationService = nodeInformationService;
            _exchangeService = exchangeService;

            var configuration = _parametersService.Current();
            _nextExchangeTime = DateTime.Now.AddMinutes(configuration.FmuApiCentralServer.ExchangeRequestInterval);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var tryCounts = 0;
            
            await Task.Delay(TimeSpan.FromMinutes(StartDelayMinutes), stoppingToken);
            
            while (!stoppingToken.IsCancellationRequested)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                if (DateTime.Now < _nextExchangeTime)
                {
                    continue;
                }
                var configuration = await _parametersService.CurrentAsync();

                if (configuration.FmuApiCentralServer.Enabled)
                {
                    var result = await ActExchange();

                    if (!result && tryCounts <= TryAfterErrorLimit) { 
                        _nextExchangeTime = DateTime.Now.AddMinutes(DelayAfterErrorMinutes);
                        tryCounts++;
                        continue;
                    }
                }

                _nextExchangeTime = DateTime.Now.AddMinutes(configuration.FmuApiCentralServer.ExchangeRequestInterval);
            }
        }

        private async Task<bool> ActExchange()
        {
            var configuration = await _parametersService.CurrentAsync();
            var data = await _nodeInformationService.Create();
            
            var exchangeResult = await _exchangeService.ActExchange(data, configuration.FmuApiCentralServer.Address);

            if (exchangeResult.IsFailure)
            {
                _logger.LogError("Exchange failed: {Error}", exchangeResult.Error);
                return false;
            }

            if (exchangeResult.Value.SoftwareUpdateAvailable)
            {
                _logger.LogInformation("New version available to download");
                await DownloadSoftwareUpdate();
            }

            if (exchangeResult.Value.SettingsUpdateAvailable)
            {
                _logger.LogInformation("New configuration ready to download from central server");
                await DownloadConfiguration();
            }
            
            return true;
        }

        private async Task DownloadConfiguration()
        {
            throw new NotImplementedException();
        }

        private async Task DownloadSoftwareUpdate()
        {
            throw new NotImplementedException();
        }
    }
}
