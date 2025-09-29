using System.Text.Json;
using CentralServerExchange.Interfaces;
using CentralServerExchange.Services;
using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.DTO.FmuApiExchangeData.Answer;
using FmuApiDomain.DTO.FmuApiExchangeData.Request;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Strings;

namespace CentralServerExchange.Workers
{
    public class CentralServerExchangeWorker : BackgroundService
    {
        private readonly ILogger<CentralServerExchangeWorker> _logger;
        
        private readonly IParametersService _parametersService;
        private readonly IExchangeService _exchangeService;
        private readonly INodeInformationService _nodeInformationService;
        private readonly ConfigurationDownloadService _configurationDownloadService;
        
        private DateTime _nextExchangeTime;

        private const int TryAfterErrorLimit = 10;
#if DEBUG
        private const int StartDelayMinutes = 1;            
#else
        private const int StartDelayMinutes = 5;        
#endif
        private const int DelayAfterErrorMinutes = 1;
        
        public CentralServerExchangeWorker(ILogger<CentralServerExchangeWorker> logger,
            IParametersService parametersService,
            INodeInformationService nodeInformationService,
            IExchangeService exchangeService,
            ConfigurationDownloadService configurationDownloadService)
        {
            _logger = logger;
            _parametersService = parametersService;
            _nodeInformationService = nodeInformationService;
            _exchangeService = exchangeService;
            _configurationDownloadService = configurationDownloadService;

            var configuration = _parametersService.Current();
            _nextExchangeTime = DateTime.Now.AddMinutes(StartDelayMinutes);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var tryCounts = 0;
            
            while (!stoppingToken.IsCancellationRequested)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                if (DateTime.Now < _nextExchangeTime)
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken).ConfigureAwait(false);
                    continue;
                }
                
                var configuration = await _parametersService.CurrentAsync().ConfigureAwait(false);

                if (configuration.FmuApiCentralServer.Enabled)
                {
                    var result = await ActExchange().ConfigureAwait(false);

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
            var configuration = await _parametersService.CurrentAsync().ConfigureAwait(false);
            var data = await _nodeInformationService.Create().ConfigureAwait(false);
            var baseAddress = $"{configuration.FmuApiCentralServer.Address}/api/FmuApiInstanceMonitoring";
            
            var exchangeResult = await _exchangeService.ActExchange(data, baseAddress).ConfigureAwait(false);

            if (exchangeResult.IsFailure)
            {
                _logger.LogError("Обмен с центральным сервером завершен с ошибкой: {Error}", exchangeResult.Error);
                return false;
            }

            if (exchangeResult.Value.SoftwareUpdateAvailable)
            {
                _logger.LogInformation("Обнаружена новая верся fmu-api для загрузки");
                await DownloadSoftwareUpdate();
            }
            
            await _configurationDownloadService.DownloadAndApply(exchangeResult.Value, baseAddress, configuration.FmuApiCentralServer.Token).ConfigureAwait(false);
            
            return true;
        }
        private async Task DownloadSoftwareUpdate()
        {
            throw new NotImplementedException();
        }
    }
}
