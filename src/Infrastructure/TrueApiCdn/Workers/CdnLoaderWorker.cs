using FmuApiDomain.Cdn;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.State.Interfaces;
using FmuApiDomain.TrueApi.Cdn;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Shared.Http;
using TrueApiCdn.Interface;

namespace TrueApiCdn.Workers
{
    public class CdnLoaderWorker : BackgroundService
    {
        private readonly IParametersService _parametersService;
        private readonly ICdnService _cdnService;
        private readonly ILogger<CdnLoaderWorker> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IApplicationState _applicationState;

        private readonly string _cdnUrl = @"https://cdn.crpt.ru/api/v4/true-api/cdn/info";
        private readonly string _healthCheckAddress = @"api/v4/true-api/cdn/health/check";

        private DateTime _nextWorkDate = DateTime.Now;
        private const int CheckInterval = 60_000;
        private const int CheckPeriodMinutes = 540;

        private Parameters _configuration;

        public CdnLoaderWorker(IParametersService parametersService, 
                               ICdnService cdnService,
                               IHttpClientFactory httpClientFactory,
                               IApplicationState applicationState,
                               ILogger<CdnLoaderWorker> logger)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _parametersService = parametersService;
            _cdnService = cdnService;
            _applicationState = applicationState;

            _configuration = _parametersService.Current();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var loadedCdnList = await _cdnService.GetCdnsAsync();
                
                if (_nextWorkDate >= DateTime.Now 
                    && loadedCdnList.Any()) 
                {
                    await Task.Delay(CheckInterval, stoppingToken);
                    continue;
                }

                _nextWorkDate = DateTime.Now.AddMinutes(CheckPeriodMinutes);
        
                _configuration = await _parametersService.CurrentAsync();
                
                if (_configuration.ServerConfig.TsPiotEnabled)
                    continue;
                
                if (!_cdnService.ShouldUpdateCdnList() & loadedCdnList.Count > 0)
                    continue;                    

                _logger.LogInformation("Загружаю список CDN");

                if (!_applicationState.IsOnline())
                {
                    _logger.LogWarning("Ошибка загрузки CDN: нет подключения к интернету");
                    continue;
                }

                var xApiKey = _configuration.OrganisationConfig.XapiKey();

                if (xApiKey == string.Empty)
                {
                    _logger.LogWarning("Не настроен XAPIKEY");
                    continue;
                }

                try
                {
                    _ = await UpdateCdnList(xApiKey);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Ошибка загрузки CDN серверов: {Message} \r\n {InnerException}", ex.Message, ex.InnerException);
                }
            }
        }

        private async Task<bool> UpdateCdnList(string xapikey)
        {
            var cdns = await LoadCdNsAsync(xapikey);
            var trueSignCdns = await CheckAllCdnsAsync(cdns, xapikey);

            if (trueSignCdns.Count > 0)
                await _cdnService.SaveCdnsAsync(trueSignCdns);

            return true;
        }

        private static Dictionary<string, string> Headers(string xapikey)
        {
            Dictionary<string, string> headers = new()
            {
                { HeaderNames.Accept, "application/json" },
                { "X-API-KEY", xapikey }
            };

            return headers;
        }

        private async Task<List<CdnHost>> LoadCdNsAsync(string xapikey)
        {
            CdnListAnswerTrueApi? cdns;

            cdns = await HttpHelpers.GetJsonFromHttpAsync<CdnListAnswerTrueApi>(_cdnUrl,
                                                                                Headers(xapikey),
                                                                                _httpClientFactory,
                                                                                TimeSpan.FromSeconds(_configuration.HttpRequestTimeouts.CdnRequestTimeout));

            cdns ??= new CdnListAnswerTrueApi();

            return cdns.Hosts;
        }

        private async Task<List<TrueSignCdn>> CheckAllCdnsAsync(List<CdnHost> cdns, string xapikey)
        {
            List<TrueSignCdn> trueSignCdns = [];

            foreach (var cdnHost in cdns)
            {
                var latency = await CheckHostHealthAsync(cdnHost, xapikey);

                if (latency < 0)
                    continue;

                TrueSignCdn cdn = new()
                {
                    Host = cdnHost.Host,
                    Latency = latency
                };

                trueSignCdns.Add(cdn);
            }

            return trueSignCdns;
        }

        private async Task<int> CheckHostHealthAsync(CdnHost cdn, string xapikey)
        {
            CdnHealth? cdnHealth = new();

            var beginCheckHealth = DateTime.Now;

            try
            {
                cdnHealth = await HttpHelpers.GetJsonFromHttpAsync<CdnHealth>($"{cdn.Host}/{_healthCheckAddress}",
                                                                              Headers(xapikey),
                                                                              _httpClientFactory,
                                                                              TimeSpan.FromSeconds(_configuration.HttpRequestTimeouts.CdnRequestTimeout));
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Ошибка проверки health check cdn сервера: {srv} {exMessage} \r\n {exInnerException}", cdn.Host, ex.Message, ex.InnerException);
                return -1;
            }

            return (int)(DateTime.Now - beginCheckHealth).Ticks;

        }
    }
}
