using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Node.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using TrueApiCdn.Interface;

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

            _exchangeService = CentralServerExchangeService.Create(_httpClient, _logger, configuration.FmuApiCentralServer.Address);
            nextExchangeTime = DateTime.Now.AddMinutes(configuration.FmuApiCentralServer.ExchangeRequestInterval);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(60_000, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                var configuration = _parametersService.Current();

                if (configuration.FmuApiCentralServer.Enabled)
                    continue;

                if (DateTime.Now < nextExchangeTime)
                    continue;

                ActExchange();

                nextExchangeTime = DateTime.Now.AddMinutes(configuration.FmuApiCentralServer.ExchangeRequestInterval);
            }
        }

        private async void ActExchange()
        {
            _logger.LogInformation("Starting exchange with central server");

            var parameters = await _parametersService.CurrentAsync();
            var cdnData = await _cdnService.GetCdnsAsync();

            var requestData = NodeDataRequest.Create(parameters.NodeName, parameters.FmuApiCentralServer.Token, parameters, cdnData);

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
