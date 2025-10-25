using FmuApiDomain.CentralServiceExchange.Interfaces;
using FmuApiDomain.Configuration.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CentralServerExchange.Workers
{
    public class CentralServerExchangeWorker : BackgroundService
    {
        private readonly ILogger<CentralServerExchangeWorker> _logger;
        
        private readonly IParametersService _parametersService;
        private readonly ICentralServerExchangeActions _exchangeActions;
        
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
            ICentralServerExchangeActions exchangeActions)
        {
            _logger = logger;
            _parametersService = parametersService;
            _exchangeActions = exchangeActions;

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
                    var result = await _exchangeActions.StartExchange().ConfigureAwait(false);

                    if (!result && tryCounts <= TryAfterErrorLimit) { 
                        _nextExchangeTime = DateTime.Now.AddMinutes(DelayAfterErrorMinutes);
                        tryCounts++;
                        continue;
                    }
                }

                tryCounts = 0;
                _nextExchangeTime = DateTime.Now.AddMinutes(configuration.FmuApiCentralServer.ExchangeRequestInterval);
                await Task.Delay(configuration.FmuApiCentralServer.ExchangeRequestInterval, stoppingToken).ConfigureAwait(false);
            }
        }
    }
}
