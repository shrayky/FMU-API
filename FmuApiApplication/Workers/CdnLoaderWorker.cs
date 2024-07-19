using FmuApiApplication.Utilites;
using FmuApiDomain.Models.Configuration.TrueSign;
using FmuApiDomain.Models.TrueSignApi.Cdn;
using FmuApiSettings;
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
      
        public CdnLoaderWorker(ILogger<CdnLoaderWorker> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (nextWorkDate >= DateTime.Now)
                {
                    await Task.Delay(10_000, stoppingToken);
                    continue;
                }

                _logger.LogInformation("Загружаю список cdn");

                nextWorkDate = DateTime.Now.AddMinutes(checkPeriodMinutes);

                if (!Constants.Online)
                {
                    _logger.LogWarning("Нет подключения к интернету");
                    continue;
                }

                string xapikey = Constants.Parametrs.OrganisationConfig.XapiKey();

                if (xapikey == string.Empty)
                {
                    _logger.LogWarning("Не настроен XAPIKEY");
                    continue;
                }

                try
                {
                    _ = await UpadteCdnList(xapikey);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Ошибка загрузки cdn серверов: {ex.Message} \r\n {ex.InnerException}");
                }
            }
        }

        private async Task<bool> UpadteCdnList(string xapikey)
        {
            Dictionary<string, string> headers = new()
            {
                { HeaderNames.Accept, "application/json" },
                { "X-API-KEY", xapikey }
            };

            CdnListAnswerTrueApi? cdns = await HttpRequestHelper.GetJsonFromHttpAsync<CdnListAnswerTrueApi>(_cdnUrl,
                                                                                                            headers,
                                                                                                            _httpClientFactory,
                                                                                                            TimeSpan.FromSeconds(Constants.Parametrs.HttpRequestTimeouts.CdnRequestTimeout));

            cdns ??= new();

            List<TrueSignCdn> trueSignCdns = [];

            foreach (CdnHost cdnHost in cdns.Hosts)
            {
                CdnHealth? cdnHealth = new();

                DateTime beginCheckHealth = DateTime.Now;

                try
                {
                    cdnHealth = await HttpRequestHelper.GetJsonFromHttpAsync<CdnHealth>($"{cdnHost.Host}/api/v4/true-api/cdn/health/check",
                                                                                            headers,
                                                                                            _httpClientFactory,
                                                                                             TimeSpan.FromSeconds(Constants.Parametrs.HttpRequestTimeouts.CdnRequestTimeout));
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
                //Constants.Parametrs.Cdn = trueSignCdns.OrderBy(p => p.Latency).ToList();
                //await Constants.Parametrs.SaveAsync(Constants.Parametrs, Constants.DataFolderPath);
            }

            return true;

        }
    }
}
