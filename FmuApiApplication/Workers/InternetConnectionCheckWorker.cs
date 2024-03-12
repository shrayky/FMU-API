using FmuApiApplication.Utilites;
using FmuApiDomain.Models.TrueSignApi.Cdn;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace FmuApiApplication.Workers
{
    public class InternetConnectionCheckWorker : BackgroundService
    {
        private readonly ILogger<InternetConnectionCheckWorker> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly int CheckPeriodMinutes = 2;
        private readonly int RequestTimeoutSeconds = 15;
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

                    if (Constants.Parametrs.HostToPing != string.Empty) 
                    {
                        try
                        {
                            var result = await HttpRequestHelper.GetHttpAsync(Constants.Parametrs.HostToPing,
                                                                              headers,
                                                                              _httpClientFactory,
                                                                              TimeSpan.FromSeconds(RequestTimeoutSeconds));

                            Constants.Online = (result != string.Empty);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Ошибка проверки доступности интеренета: {ex.Message}");
                            Constants.Online = false;
                        }
                    }
                }

                await Task.Delay(60_000, stoppingToken);
            }
        }
    }
}
