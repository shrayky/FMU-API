using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Options.TrueSign;
using FmuApiDomain.TrueSignApi.Cdn;
using FmuApiSettings;
using Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Shared.Http;

namespace FmuApiApplication.Workers
{
    public class CdnLoaderWorker : BackgroundService
    {
        private readonly IParametersService _parametersService;
        private readonly ILogger<CdnLoaderWorker> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly string _cdnUrl = @"https://cdn.crpt.ru/api/v4/true-api/cdn/info";
        private readonly string _healthCheckAddress = @"api/v4/true-api/cdn/health/check";
        private DateTime nextWorkDate = DateTime.Now;
        private readonly int _checkInterval = 10_000;
        private readonly int checkPeriodMinutes = 120;
        private Parameters _configuration;

        public CdnLoaderWorker(IParametersService parametersService, IHttpClientFactory httpClientFactory, ILogger<CdnLoaderWorker> logger)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _parametersService = parametersService;

            _configuration = _parametersService.Current();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (nextWorkDate >= DateTime.Now)
                {
                    await Task.Delay(_checkInterval, stoppingToken);
                    continue;
                }

                _configuration = _parametersService.Current();

                _logger.LogInformation("Загружаю список cdn");

                nextWorkDate = DateTime.Now.AddMinutes(checkPeriodMinutes);

                if (!Constants.Online)
                {
                    _logger.LogWarning("Нет подключения к интернету");
                    continue;
                }

                string xApiKey = _configuration.OrganisationConfig.XapiKey();

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
                    _logger.LogWarning($"Ошибка загрузки cdn серверов: {ex.Message} \r\n {ex.InnerException}");
                }
            }
        }

        private async Task<bool> UpdateCdnList(string xapikey)
        {
            Dictionary<string, string> headers = new()
            {
                { HeaderNames.Accept, "application/json" },
                { "X-API-KEY", xapikey }
            };

            CdnListAnswerTrueApi? cdns = await HttpHelpers.GetJsonFromHttpAsync<CdnListAnswerTrueApi>(_cdnUrl,
                                                                                                      headers,
                                                                                                      _httpClientFactory,
                                                                                                      TimeSpan.FromSeconds(_configuration.HttpRequestTimeouts.CdnRequestTimeout));

            cdns ??= new();

            List<TrueSignCdn> trueSignCdns = [];

            foreach (CdnHost cdnHost in cdns.Hosts)
            {
                CdnHealth? cdnHealth = new();

                DateTime beginCheckHealth = DateTime.Now;

                try
                {
                    cdnHealth = await HttpHelpers.GetJsonFromHttpAsync<CdnHealth>($"{cdnHost.Host}/{_healthCheckAddress}",
                                                                                  headers,
                                                                                  _httpClientFactory,
                                                                                  TimeSpan.FromSeconds(_configuration.HttpRequestTimeouts.CdnRequestTimeout));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Ошибка проверки healthcheck cdn сервера: {srv} {exMessage} \r\n {exInnerException}", cdnHost.Host, ex.Message, ex.InnerException);
                    continue;
                }

                if (cdnHealth is null)
                    continue;

                TrueSignCdn cdn = new()
                {
                    Host = cdnHost.Host,
                    Latency = (int)(DateTime.Now - beginCheckHealth).Ticks
                };

                trueSignCdns.Add(cdn);
            }

            if (trueSignCdns.Count > 0)
            {
                Constants.Cdn.List = trueSignCdns.OrderBy(p => p.Latency).ToList();
                await Constants.Cdn.SaveAsync(Constants.DataFolderPath);
            }

            return true;

        }
    }
}
