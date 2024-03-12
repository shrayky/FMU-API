using FmuApiApplication.Utilites;
using FmuApiDomain.Models.TrueSignTokenService;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace FmuApiApplication.Workers
{
    public class TrueSignTokenServiceLoader : BackgroundService
    {
        private readonly ILogger<TrueSignTokenServiceLoader> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private DateTime nextWorkDate = DateTime.Now;
        private readonly int checkPeriodMinutes = 120;
        private readonly int requestTimeoutSeconds = 15;

        public TrueSignTokenServiceLoader(IHttpClientFactory httpClientFactory, ILogger<TrueSignTokenServiceLoader> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Dictionary<string, string> headers = new()
            {
                { HeaderNames.Accept, "application/json" },
            };

            while (!stoppingToken.IsCancellationRequested)
            {
                if (nextWorkDate <= DateTime.Now)
                {
                    nextWorkDate = DateTime.Now.AddMinutes(checkPeriodMinutes);

                    if (Constants.Parametrs.TrueSignTokenService.ConnectionAddres == string.Empty)
                        continue;

                    TrueSignTokenDataPacket? packet = new();

                    try
                    {
                        packet = await HttpRequestHelper.GetJsonFromHttpAsync<TrueSignTokenDataPacket>(Constants.Parametrs.TrueSignTokenService.ConnectionAddres,
                                                                                                       headers,
                                                                                                       _httpClientFactory,
                                                                                                       TimeSpan.FromSeconds(requestTimeoutSeconds));
                    }
                    catch (Exception ex)
                    {
                            _logger.LogWarning($"Ошибка получения токена для честного знака {ex.Message}");
                            Constants.Online = false;
                    }

                    if (packet != null) 
                    {
                        Constants.Parametrs.SignData = new()
                        {
                            Signature = packet.Data.Token,
                            Expired = packet.Data.Expired
                        };

                    _logger.LogInformation($"Получен токен честного знака.");

                    }

                }

                await Task.Delay(60_000, stoppingToken);
            }
        }
    }
}
