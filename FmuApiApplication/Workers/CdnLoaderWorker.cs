using FmuApiApplication.Utilites;
using FmuApiDomain.Models.Configuration;
using FmuApiDomain.Models.TrueSignApi.Cdn;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace FmuApiApplication.Workers
{
    public class CdnLoaderWorker : BackgroundService
    {
        private readonly string _cdnUrl = @"https://cdn.crpt.ru/api/v4/true-api/cdn/info";
        
        private readonly ILogger<CdnLoaderWorker> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private DateTime nextWorkDate = DateTime.Now;
        private readonly int checkPeriodMinutes = 120;
        private readonly int requestTimeoutSeconds = 15;

        public CdnLoaderWorker(ILogger<CdnLoaderWorker> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (nextWorkDate <= DateTime.Now)
                {
                    nextWorkDate = DateTime.Now.AddMinutes(checkPeriodMinutes);

                    if (!Constants.Online)
                        continue;

                    if (Constants.Parametrs.XAPIKEY == string.Empty)
                        continue;

                    try
                    {
                        _ = await UpadteCdnList();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Ошибка загрузки cdn серверов: {ex.Message}");
                    }
                }

                await Task.Delay(10_000, stoppingToken);
            }
        }

        private async Task<bool> UpadteCdnList()
        {
            Dictionary<string, string> headers = new()
            {
                { HeaderNames.Accept, "application/json" },
                { "X-API-KEY", Constants.Parametrs.XAPIKEY }
            };

            CdnListAnswerTrueApi? cdns = await HttpRequestHelper.GetJsonFromHttpAsync<CdnListAnswerTrueApi>(_cdnUrl,
                                                                                                            headers,
                                                                                                            _httpClientFactory,
                                                                                                            TimeSpan.FromSeconds(requestTimeoutSeconds));

            if (cdns is null)
                cdns = new();

            List<TrueSignCdn> trueSignCdns = new();

            foreach (CdnHost cdnHost in cdns.Hosts)
            {
                CdnHealth? cdnHealth;

                DateTime beginCheckHealth = DateTime.Now;

                cdnHealth = await HttpRequestHelper.GetJsonFromHttpAsync<CdnHealth>($"{cdnHost.Host}/api/v4/true-api/cdn/health/check",
                                                                                        headers,
                                                                                        _httpClientFactory,
                                                                                         TimeSpan.FromSeconds(requestTimeoutSeconds));

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
                Constants.Parametrs.Cdn = trueSignCdns.OrderBy(p => p.Latency).ToList();
                await Constants.Parametrs.SaveAsync(Constants.Parametrs);
            }

            return true;

        }
    }
}
