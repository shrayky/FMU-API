using FmuApiDomain.Configuration;
using FmuApiDomain.TrueSignTokenService;
using FmuApiSettings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Shared.Http;

namespace FmuApiApplication.Workers
{
    public class TrueSignTokenServiceLoaderWorker : BackgroundService
    {
        private readonly IParametersService _parametersService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TrueSignTokenServiceLoaderWorker> _logger;

        private DateTime nextWorkDate = DateTime.Now;
        private readonly int checkPeriodMinutes = 120;
        private readonly int requestTimeoutSeconds = 15;
        private readonly int _checkInterval = 60_000;
        private Parameters _configuration;

        public TrueSignTokenServiceLoaderWorker(IParametersService parametersService, IHttpClientFactory httpClientFactory, ILogger<TrueSignTokenServiceLoaderWorker> logger)
        {
            _parametersService = parametersService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            _configuration = _parametersService.Current();
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Dictionary<string, string> headers = new()
            {
                { HeaderNames.Accept, "application/json" },
            };

            while (!stoppingToken.IsCancellationRequested)
            {
                if (nextWorkDate >= DateTime.Now)
                    continue;

                nextWorkDate = DateTime.Now.AddMinutes(checkPeriodMinutes);

                TrueSignTokenDataPacket? packet = new();

                _configuration = _parametersService.Current();

                try
                {
                    packet = await HttpHelpers.GetJsonFromHttpAsync<TrueSignTokenDataPacket>(_configuration.TrueSignTokenService.ConnectionAddres,
                                                                                             headers,
                                                                                             _httpClientFactory,
                                                                                             TimeSpan.FromSeconds(requestTimeoutSeconds));
                }
                catch (Exception ex)
                {
                        _logger.LogWarning("Ошибка получения токена для честного знака {err}", ex.Message);
                        Constants.Online = false;
                }

                if (packet != null) 
                {
                    Constants.TrueApiToken= new()
                    {
                        Signature = packet.Data.Token,
                        Expired = packet.Data.Expired
                    };

                    _logger.LogInformation("Получен токен честного знака.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }
    }
}
