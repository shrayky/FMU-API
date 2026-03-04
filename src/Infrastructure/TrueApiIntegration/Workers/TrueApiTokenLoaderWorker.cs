using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using TrueApiIntegration.Interfaces;

namespace TrueApiIntegration.Workers;
public class TrueApiTokenLoaderWorker : BackgroundService
{
    private readonly ILogger<TrueApiTokenLoaderWorker> _logger;
    private readonly IParametersService _parametersService;
    private readonly IApplicationState _applicationState;
    private readonly IAuthService _authService;

    private DateTime _nextWorkDate = DateTime.Now;
    private const int CheckIntervalMinutes = 10;
    private const int TokenLifeInHouer = 8;
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
        await Task.Delay(TimeSpan.FromMinutes(StartDelayMinutes), stoppingToken).ConfigureAwait(false);

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

                var currentToken = _applicationState.TrueApiToken(organisation.INN);

                if (currentToken != string.Empty)
                    continue;

                var token = await _authService.GenerateToken(organisation.INN, "12345678");

                if (token == string.Empty)
                    continue;

                var tokenLifeUntil = DateAndTime.Now.AddHours(TokenLifeInHouer);

                _applicationState.UpdateTrueApiToken(organisation.INN, token, tokenLifeUntil);

                _logger.LogInformation("Для {inn} получен новый токен, который действует до {tokenLifeUntil}", organisation.INN, tokenLifeUntil);
            }

            _nextWorkDate = DateTime.Now.AddMinutes(CheckIntervalMinutes);

        }

    }
}

