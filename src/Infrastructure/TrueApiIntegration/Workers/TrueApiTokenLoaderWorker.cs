using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TrueApiIntegration.Interfaces;

namespace TrueApiIntegration.Workers;

/// <summary>
/// Запрашивает UUID-токен True API для организаций с локальным подключением.
/// </summary>
public class TrueApiTokenLoaderWorker : BackgroundService
{
    private readonly ILogger<TrueApiTokenLoaderWorker> _logger;
    private readonly IParametersService _parametersService;
    private readonly IApplicationState _applicationState;
    private readonly IAuthService _authService;

    private DateTime _nextWorkDate = DateTime.Now;
    private const int CheckIntervalMinutes = 10;
    private const int StartDelayMinutes = 2;

    public TrueApiTokenLoaderWorker(ILogger<TrueApiTokenLoaderWorker> logger,
        IParametersService parametersService,
        IAuthService authService,
        IApplicationState applicationState)
    {
        _logger = logger;
        _parametersService = parametersService;
        _authService = authService;
        _applicationState = applicationState;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
#if !DEBUG

        await Task.Delay(TimeSpan.FromMinutes(StartDelayMinutes), stoppingToken).ConfigureAwait(false);
#endif
        while (!stoppingToken.IsCancellationRequested)
        {
            if (DateTime.Now < _nextWorkDate)
            {
                await Task.Delay(TimeSpan.FromMinutes(CheckIntervalMinutes), stoppingToken).ConfigureAwait(false);
                continue;
            }

            var configuration = await _parametersService.CurrentAsync();

            foreach (var organisation in configuration.OrganisationConfig.PrintGroups)
            {
                if (string.IsNullOrEmpty(organisation.INN))
                    continue;

                if (!organisation.TrueApiIntegrationSettings.ShouldRequestTokenViaCryptoPro())
                    continue;

                var tokenData = _applicationState.TrueApiToken(organisation.INN);

                if (tokenData.Token != string.Empty)
                    continue;

                _logger.LogDebug("Начинаю получать токен true api для {inn}", organisation.INN);

                var trueApiSettings = organisation.TrueApiIntegrationSettings;

                var session = await _authService.GenerateToken(organisation.INN, trueApiSettings.Password, trueApiSettings.DigitalSignature);
                if (session.IsFailure)
                {
                    _logger.LogWarning("Не удалось получить токен true api для {inn}: {error}", organisation.INN, session.Error);
                    continue;
                }

                _applicationState.UpdateTrueApiToken(organisation.INN, session.Value.Token, session.Value.LiveUntil);

                _logger.LogInformation("Для {inn} получен новый токен, который действует до {tokenLifeUntil}", organisation.INN, session.Value.LiveUntil);
            }

            _nextWorkDate = DateTime.Now.AddMinutes(CheckIntervalMinutes);

        }

    }
}

