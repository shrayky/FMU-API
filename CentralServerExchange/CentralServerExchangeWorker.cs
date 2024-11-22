using Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodeInformation;
using System.Net.Http.Headers;

namespace CentralServerExchange
{
    public class CentralServerExchangeWorker : BackgroundService
    {
        private readonly ILogger<CentralServerExchangeWorker> _logger;
        private HttpClient _httpClient { get; init; }
        private readonly IParametersService _parametersService;
        private readonly ICdnService _cdnService;
        private readonly CentralServerExchangeService _exchangeService;
        private DateTime nextExchangeTime = DateTime.MaxValue;

        public CentralServerExchangeWorker(ILogger<CentralServerExchangeWorker> logger, HttpClient httpClient, IParametersService parametersService, ICdnService cdnService)
        {
            _logger = logger;
            _httpClient = httpClient;
            _parametersService = parametersService;
            _cdnService = cdnService;

            var configuration = _parametersService.Current();

            _exchangeService = CentralServerExchangeService.Create(_httpClient, _logger, configuration.CentralServerConnectionSettings.Adres);
            nextExchangeTime = DateTime.Now.AddMinutes(configuration.CentralServerConnectionSettings.ExchangeRequestInterval);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(60_000, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                var configuration = _parametersService.Current();

                if (configuration.CentralServerConnectionSettings.Enabled)
                    continue;

                if (DateTime.Now < nextExchangeTime)
                    continue;

                ActExchange();

                nextExchangeTime = DateTime.Now.AddMinutes(configuration.CentralServerConnectionSettings.ExchangeRequestInterval);
            }
        }

        private async void ActExchange()
        {
            _logger.LogInformation("Starting exchange with central server");

            var parameters = await _parametersService.CurrentAsync();
            var cdnData = await _cdnService.CurrentAsync();

            var requestData = NodeDataRequest.Create(parameters.NodeName, parameters.CentralServerConnectionSettings.Token, parameters, cdnData);

            if (requestData.IsFailure)
            {
                _logger.LogError(requestData.Error);
                return;
            }

            var exchangeResult = await _exchangeService.ActExchange(requestData.Value);

            if (exchangeResult.IsFailure)
            {
                _logger.LogError("Exchange failed: {Error}", exchangeResult.Error);
                return;
            }

            if (exchangeResult.Value.SoftwareUpdateAvailable)
            {
                _logger.LogInformation("New version available to download");
                await DownloadSoftwareUpdate();
            }

            if (exchangeResult.Value.ConfigurationUpdateAvailable)
            {
                _logger.LogInformation("New configuration ready to download from central server");
                await DownloadConfigureation();
            }
        }

        private async Task DownloadConfigureation()
        {
            throw new NotImplementedException();
        }

        private async Task DownloadSoftwareUpdate()
        {
            throw new NotImplementedException();
        }

        public static void AddService(IServiceCollection services)
        {
            services.AddHostedService<CentralServerExchangeWorker>();

            services.AddHttpClient<CentralServerExchangeService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            });
        }

    }
}
