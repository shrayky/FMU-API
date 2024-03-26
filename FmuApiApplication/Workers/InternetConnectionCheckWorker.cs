using FmuApiApplication.Utilites;
using FmuApiDomain.Models.TrueSignApi.Cdn;
using FmuApiSettings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Json;

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
                if (nextWorkDate <= DateTime.Now)
                {
                    nextWorkDate = DateTime.Now.AddMinutes(CheckPeriodMinutes);

                    try
                    {
                        var client = _httpClientFactory.CreateClient("internetCheck");
                        var answer = await client.GetAsync("");

                        Constants.Online = (answer.StatusCode == System.Net.HttpStatusCode.OK);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Ошибка проверки доступности интеренета: {err}", ex.Message);
                        Constants.Online = false;
                    }
                }

                await Task.Delay(60_000, stoppingToken);
            }
        }
    }
}
