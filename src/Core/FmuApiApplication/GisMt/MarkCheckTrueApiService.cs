using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.GisMt;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Models;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.GisMt;

[AutoRegisterService(ServiceLifetime.Scoped)]
public class MarkCheckTrueApiService(
    ILogger<MarkCheckTrueApiService> logger,
    IParametersService parametersService,
    IApplicationState applicationState,
    IGisMtCisesClient cisesClient) : IMarkCheckTrueApiService
{
    private readonly ILogger<MarkCheckTrueApiService> _logger = logger;
    private readonly IParametersService _parametersService = parametersService;
    private readonly IApplicationState _applicationState = applicationState;
    private readonly IGisMtCisesClient _cisesClient = cisesClient;

    public async Task<MarkCheckTrueApiResult> CisesInfo(
        string inn,
        IReadOnlyList<string> cises,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = await _parametersService.CurrentAsync();
            var organisation = GisMtOrganisationResolver.Find(parameters, inn);

            if (organisation is null)
            {
                return MarkCheckTrueApiResult.WithStatus(
                    MarkCheckTrueApiStatuses.Disabled,
                    $"Организация с ИНН {inn} не найдена в настройках");
            }

            if (!organisation.TrueApiIntegrationSettings.Enable)
            {
                return MarkCheckTrueApiResult.WithStatus(
                    MarkCheckTrueApiStatuses.Disabled,
                    $"True API не подключён для ИНН {organisation.INN}");
            }

            if (!_applicationState.IsOnline())
            {
                return MarkCheckTrueApiResult.WithStatus(
                    MarkCheckTrueApiStatuses.Offline,
                    "Нет доступа к интернету");
            }

            var token = _applicationState.TrueApiToken(organisation.INN).Token;
            if (string.IsNullOrWhiteSpace(token))
            {
                return MarkCheckTrueApiResult.WithStatus(
                    MarkCheckTrueApiStatuses.NoToken,
                    $"Токен True API не получен для ИНН {organisation.INN}");
            }

            var cisList = cises
                .Select(GisMtCisNormalizer.ToCis)
                .Where(x => x.Length > 0)
                .ToList();

            var cisInfo = await _cisesClient.CisesInfo(
                token,
                cisList,
                productGroup: null,
                cancellationToken);

            if (cisInfo.IsFailure)
            {
                return MarkCheckTrueApiResult.WithStatus(
                    MarkCheckTrueApiStatuses.Error,
                    cisInfo.Error);
            }

            return MarkCheckTrueApiResult.Success(cisInfo.Value);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка запроса cises/info для проверки марки, ИНН {Inn}", inn);
            return MarkCheckTrueApiResult.WithStatus(
                MarkCheckTrueApiStatuses.Error,
                ex.Message);
        }
    }
}
