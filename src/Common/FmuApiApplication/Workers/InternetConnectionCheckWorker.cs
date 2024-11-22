using FmuApiSettings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace FmuApiApplication.Workers
{
    public class InternetConnectionCheckWorker : BackgroundService
    {
        private readonly ILogger<InternetConnectionCheckWorker> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly int CheckPeriodMinutes = 2;
        private DateTime nextWorkDate = DateTime.Now;

        public InternetConnectionCheckWorker(ILogger<InternetConnectionCheckWorker> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
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
                    await Task.Delay(60_000, stoppingToken);
                    continue;
                }
                 
                nextWorkDate = DateTime.Now.AddMinutes(CheckPeriodMinutes);

                bool online = false;

                foreach (var siteAdres in Constants.Parametrs.HostsToPing)
                {
                    var adr = siteAdres.Value.Trim();

                    if (adr == string.Empty)
                        continue;

                    if (!adr.StartsWith("https://"))
                        adr = $"https://{adr}";

                    online = await CheckAsyns(adr);

                    if (online)
                        break;
                }

                Constants.Online = online;
                
            }
        }

        private async Task<bool> CheckAsyns(string siteAdres)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("internetCheck");
                client.Timeout = TimeSpan.FromSeconds(Constants.Parametrs.HttpRequestTimeouts.CheckInternetConnectionTimeout);

                client.BaseAddress = new Uri(siteAdres);

                var answer = await client.GetAsync("");

                return answer.StatusCode == HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Ошибка проверки доступности интеренета: {err}", ex.Message);
                return false;
            }
        }
    }
}
