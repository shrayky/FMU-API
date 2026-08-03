using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options.Organization;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.GisMt;

/// <summary>
/// Управление товарными группами организации в ГИС МТ.
/// </summary>
[AutoRegisterService(ServiceLifetime.Scoped)]
public class GisMtProductGroupsService(
    ILogger<GisMtProductGroupsService> logger,
    IParametersService parametersService,
    IApplicationState applicationState,
    IGisMtParticipantsClient participantsClient) : IGisMtProductGroupsService
{
    private readonly ILogger<GisMtProductGroupsService> _logger = logger;
    private readonly IParametersService _parametersService = parametersService;
    private readonly IApplicationState _applicationState = applicationState;
    private readonly IGisMtParticipantsClient _participantsClient = participantsClient;

    /// <summary>
    /// Загружает ProductGroups из /participants и сохраняет в конфигурацию.
    /// </summary>
    public async Task<Result<IReadOnlyList<string>>> Refresh(string inn, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = await _parametersService.CurrentAsync();
            var organisation = GisMtOrganisationResolver.Find(parameters, inn);

            if (organisation is null)
                return Result.Failure<IReadOnlyList<string>>($"Организация с ИНН {inn} не найдена");

            var token = _applicationState.TrueApiToken(organisation.INN).Token;
            if (string.IsNullOrWhiteSpace(token))
                return Result.Failure<IReadOnlyList<string>>($"Нет Bearer-токена для ИНН {organisation.INN}");

            return await RefreshInternal(organisation, token, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обновления товарных групп для {Inn}", inn);
            return Result.Failure<IReadOnlyList<string>>(ex.Message);
        }
    }

    /// <summary>
    /// Возвращает сохранённые группы; при отсутствии — загружает из ГИС МТ.
    /// </summary>
    public async Task<Result<IReadOnlyList<string>>> GetOrRefresh(string inn, CancellationToken cancellationToken = default)
    {
        var parameters = await _parametersService.CurrentAsync();
        var organisation = GisMtOrganisationResolver.Find(parameters, inn);

        if (organisation is null)
            return Result.Failure<IReadOnlyList<string>>($"Организация с ИНН {inn} не найдена");

        if (organisation.TrueApiIntegrationSettings.ProductGroups.Count > 0)
            return Result.Success<IReadOnlyList<string>>(organisation.TrueApiIntegrationSettings.ProductGroups);

        return await Refresh(inn, cancellationToken);
    }

    private async Task<Result<IReadOnlyList<string>>> RefreshInternal(
        PrintGroupData organisation,
        string token,
        CancellationToken cancellationToken)
    {
        var participants = await _participantsClient.ParticipantsInfo(token, organisation.INN, cancellationToken);
        if (participants.IsFailure)
            return Result.Failure<IReadOnlyList<string>>(participants.Error);

        var groups = participants.Value
            .SelectMany(p => p.ProductGroups)
            .Where(g => !string.IsNullOrWhiteSpace(g))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (groups.Count == 0)
            return Result.Failure<IReadOnlyList<string>>($"Для ИНН {organisation.INN} не найдены товарные группы");

        var parameters = await _parametersService.CurrentAsync();
        var target = GisMtOrganisationResolver.Find(parameters, organisation.INN);
        if (target is null)
            return Result.Failure<IReadOnlyList<string>>($"Организация с ИНН {organisation.INN} не найдена");

        target.TrueApiIntegrationSettings.ProductGroups = groups;
        await _parametersService.UpdateAsync(parameters);

        _logger.LogInformation("Для {Inn} сохранены товарные группы: {Groups}", organisation.INN, string.Join(", ", groups));
        return Result.Success<IReadOnlyList<string>>(groups);
    }
}
