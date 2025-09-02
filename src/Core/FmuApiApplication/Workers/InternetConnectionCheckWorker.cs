using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;

namespace FmuApiApplication.Workers
{
    public class InternetConnectionCheckWorker : BackgroundService
    {
        private readonly IParametersService _parametersService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IApplicationState _applicationState;
        private readonly ILogger<InternetConnectionCheckWorker> _logger;
        
        private readonly int _checkPeriodMinutes = 2;
        private DateTime _nextWorkDate = DateTime.Now;
        private readonly int _checkInterval = 60_000;
        private Parameters _configuration;

        public InternetConnectionCheckWorker(IParametersService parametersService,
            IHttpClientFactory httpClientFactory,
            IApplicationState applicationState,
            ILogger<InternetConnectionCheckWorker> logger)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _parametersService = parametersService;
            _applicationState = applicationState;

            _configuration = _parametersService.Current();
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_nextWorkDate >= DateTime.Now)
                {
                    await Task.Delay(_checkInterval, stoppingToken);
                    continue;
                }

                _configuration = await _parametersService.CurrentAsync();

                _nextWorkDate = DateTime.Now.AddMinutes(_checkPeriodMinutes);

                var online = false;

                var hosts = _configuration.HostsToPing
                    .Select(address => address.Value.Trim())
                    .Where(address => address != string.Empty)
                    .ToList();
                
                foreach (var address in hosts)
                {
                    if (address.StartsWith("http") || address.StartsWith("https"))
                        online = await CheckAsync(address);
                    else
                        online = await CheckAsync($"https://{address}");

                    if (online)
                        break;
                }

                _applicationState.SetOnlineStatus(online);
                
            }
        }

        private async Task<bool> CheckAsync(string siteAddress)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("internetCheck");
                client.Timeout = TimeSpan.FromSeconds(_configuration.HttpRequestTimeouts.CheckInternetConnectionTimeout);

                client.BaseAddress = new Uri(siteAddress);

                var answer = await client.GetAsync("");

                return answer.StatusCode == HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Ошибка проверки доступности интернета: {err}", ex.Message);
                return false;
            }
        }
    }
}
