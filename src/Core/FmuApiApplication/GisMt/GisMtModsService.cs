using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Models;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FmuApiApplication.GisMt;

/// <summary>
/// Места осуществления деятельности организации для полей КПП и ФИАС.
/// </summary>
[AutoRegisterService(ServiceLifetime.Scoped)]
public class GisMtModsService : IGisMtModsService
{
    private const string TokenNotReadyMessage =
        "Токен ещё не готов. Он получается через 2 минуты после старта службы, а потом обновляется каждые 10 минут.";

    private readonly IParametersService _parametersService;
    private readonly IApplicationState _applicationState;
    private readonly IGisMtModsClient _modsClient;

    public GisMtModsService(
        IParametersService parametersService,
        IApplicationState applicationState,
        IGisMtModsClient modsClient)
    {
        _parametersService = parametersService;
        _applicationState = applicationState;
        _modsClient = modsClient;
    }

    public async Task<Result<IReadOnlyList<GisMtModInfo>>> Places(string inn, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(inn))
            return Result.Failure<IReadOnlyList<GisMtModInfo>>("Укажите ИНН организации");

        var parameters = await _parametersService.CurrentAsync();
        var organisation = GisMtOrganisationResolver.Find(parameters, inn);

        if (organisation is null)
            return Result.Failure<IReadOnlyList<GisMtModInfo>>($"Организация с ИНН {inn} не найдена");

        var token = _applicationState.TrueApiToken(organisation.INN).Token;

        if (string.IsNullOrWhiteSpace(token))
            return Result.Failure<IReadOnlyList<GisMtModInfo>>(TokenNotReadyMessage);

        var places = await _modsClient.Places(token, organisation.INN, cancellationToken);

        if (places.IsFailure)
            return Result.Failure<IReadOnlyList<GisMtModInfo>>(places.Error);

        if (places.Value.Count == 0)
            return Result.Failure<IReadOnlyList<GisMtModInfo>>($"Места осуществления деятельности для ИНН {organisation.INN} не найдены");

        return Result.Success<IReadOnlyList<GisMtModInfo>>(places.Value);
    }
}
