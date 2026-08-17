using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options.Organization;
using FmuApiDomain.LocalModule.Enums;
using FmuApiDomain.LocalModule.Models;
using FmuApiDomain.State.Interfaces;
using LocalModuleIntegration.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LocalModuleIntegration.Workers;

public class LocalModuleStatusWorker : BackgroundService
{
    private readonly ILogger<LocalModuleStatusWorker> _logger;
    private readonly ILocalModuleService _localModuleService;
    private readonly IParametersService _parametersService;
    private readonly IApplicationState _applicationState;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10);

    public LocalModuleStatusWorker(
        ILogger<LocalModuleStatusWorker> logger,
        ILocalModuleService localModuleService,
        IParametersService parametersService,
        IApplicationState applicationState)
    {
        _logger = logger;
        _localModuleService = localModuleService;
        _parametersService = parametersService;
        _applicationState = applicationState;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_checkInterval, stoppingToken);
            await CheckLocalModuleStatuses(stoppingToken);
        }
    }

    private async Task CheckLocalModuleStatuses(CancellationToken stoppingToken)
    {
        var config = await _parametersService.CurrentAsync();

        foreach (var organization in config.OrganisationConfig.PrintGroups)
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            if (!organization.LocalModuleConnection.Enable)
                continue;

            LocalModuleState state = new();
            LocalModuleStatus currentStatus;

            try
            {
                state = await _localModuleService.StateAsync(organization.LocalModuleConnection);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Ошибка проверки статуса локального модуля для организации {OrganizationId}, причина: {ErrorReason} ",
                                    organization.Id,
                                    ex.Message);
            }

            var lastStatus = _applicationState.OrganizationLocalModuleStatus(organization.Id);

            _applicationState.UpdateOrganizationLocalModuleInformation(organization.Id, state);

            currentStatus = state.Status;

            if (lastStatus == currentStatus)
                continue;

            if (currentStatus != LocalModuleStatus.Ready)
                _logger.LogError("Изменение статуса ЛМ для организации {OrganizationId}: {OldStatus} -> {NewStatus}",
                    organization.Id,
                    lastStatus.ToString(),
                    currentStatus.ToString()
                );
            else
                _logger.LogInformation(
                        "Изменение статуса ЛМ для организации {OrganizationId}: {OldStatus} -> {NewStatus}",
                        organization.Id,
                        lastStatus.ToString(),
                        currentStatus.ToString()
                    );

            _applicationState.UpdateOrganizationLocalModuleStatus(organization.Id, currentStatus);

            await TryAutoInitializeOnSyncError(organization, currentStatus, config);
        }
    }

    private async Task TryAutoInitializeOnSyncError(PrintGroupData organization, LocalModuleStatus currentStatus, Parameters config)
    {
        if (currentStatus != LocalModuleStatus.SyncError)
            return;

        if (!config.ServerConfig.LocalModuleGeneral.AutoInitializeOnSyncError)
            return;

        _logger.LogInformation("Автоинициализация ЛМ для организации {OrganizationId} из-за статуса sync_error", organization.Id);

        var initResult = await _localModuleService.InitializeAsync(organization.LocalModuleConnection, organization.XAPIKEY);

        if (!initResult)
        {
            _logger.LogError("Автоинициализация ЛМ для организации {OrganizationId} не выполнена", organization.Id);
            return;
        }

        _applicationState.UpdateOrganizationLocalModuleStatus(organization.Id, LocalModuleStatus.Initialization);
    }
}
