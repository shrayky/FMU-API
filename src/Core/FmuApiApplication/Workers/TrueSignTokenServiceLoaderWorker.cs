using FmuApiDomain.Authentication.Models;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.State.Interfaces;
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
        private readonly IApplicationState _applicationState;
        private readonly ILogger<TrueSignTokenServiceLoaderWorker> _logger;

        private DateTime nextWorkDate = DateTime.Now;
        private readonly int checkPeriodMinutes = 120;
        private readonly int requestTimeoutSeconds = 15;
        private readonly int _checkInterval = 60_000;
        private Parameters _configuration;

        public TrueSignTokenServiceLoaderWorker(IParametersService parametersService,
                                                IHttpClientFactory httpClientFactory,
                                                IApplicationState applicationState,
                                                ILogger<TrueSignTokenServiceLoaderWorker> logger)
        {
            _parametersService = parametersService;
            _httpClientFactory = httpClientFactory;
            _applicationState = applicationState;
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

                TokenDataPacket? packet = new();

                _configuration = _parametersService.Current();

                try
                {
                    packet = await HttpHelpers.GetJsonFromHttpAsync<TokenDataPacket>(_configuration.TrueSignTokenService.ConnectionAddress,
                                                                                             headers,
                                                                                             _httpClientFactory,
                                                                                             TimeSpan.FromSeconds(requestTimeoutSeconds));
                }
                catch (Exception ex)
                {
                        _logger.LogWarning("Ошибка получения токена для честного знака {err}", ex.Message);
                        _applicationState.SetOnlineStatus(false);
                }

                if (packet != null) 
                {

                    _applicationState.UpdateTrueApiToken(packet.Data);

                    _logger.LogInformation("Получен токен честного знака.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }
    }
}
