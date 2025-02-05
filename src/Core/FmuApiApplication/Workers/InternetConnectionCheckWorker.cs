using FmuApiApplication.Services.State;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System.Net;

namespace FmuApiApplication.Workers
{
    public class InternetConnectionCheckWorker : BackgroundService
    {
        private readonly IParametersService _parametersService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IApplicationState _applicationState;
        private readonly ILogger<InternetConnectionCheckWorker> _logger;
        
        private readonly int CheckPeriodMinutes = 2;
        private DateTime nextWorkDate = DateTime.Now;
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
            Dictionary<string, string> headers = new()
            {
                { HeaderNames.Accept, "text/html" },
            };

            while (!stoppingToken.IsCancellationRequested)
            {
                if (nextWorkDate >= DateTime.Now)
                {
                    await Task.Delay(_checkInterval, stoppingToken);
                    continue;
                }

                _configuration = _parametersService.Current();

                nextWorkDate = DateTime.Now.AddMinutes(CheckPeriodMinutes);

                bool online = false;

                foreach (var siteAddress in _configuration.HostsToPing)
                {
                    var adr = siteAddress.Value.Trim();

                    if (adr == string.Empty)
                        continue;

                    if (!adr.StartsWith("https://"))
                        adr = $"https://{adr}";

                    online = await CheckAsync(adr);

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
