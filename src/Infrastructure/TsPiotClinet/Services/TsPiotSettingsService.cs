using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options.Organization;
using FmuApiDomain.Documents;
using FmuApiDomain.TsPiot.Interfaces;
using FmuApiDomain.TsPiot.Models;
using Microsoft.Extensions.Logging;
using TsPiotClinet.Models;

namespace TsPiotClinet.Services;

public class TsPiotSettingsService(
    ILogger<TsPiotSettingsService> logger,
    IParametersService parametersService,
    TsPiotEspApiService tsPiotEspApiService) : IPiotSettingsService
{
    private readonly ILogger<TsPiotSettingsService> _logger = logger;
    private readonly IParametersService _parametersService = parametersService;
    private readonly TsPiotEspApiService _tsPiotEspApiService = tsPiotEspApiService;

    public async Task<Result<PiotSettingsPushResult>> PushSettings(
        PiotDeviceSettings settings,
        CancellationToken cancellationToken)
    {
        var appSettings = await _parametersService.CurrentAsync();
        var connections = DistinctConnections(appSettings.OrganisationConfig.PrintGroups);

        var updated = 0;
        var failed = 0;
        var errors = new List<string>();

        foreach (var connection in connections)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var instancesResult = await _tsPiotEspApiService.Instances(connection);
            if (instancesResult.IsFailure)
            {
                failed++;
                errors.Add($"{connection.Host}: {instancesResult.Error}");
                _logger.LogWarning("Не удалось получить инстансы ТС ПИоТ {Host}: {Error}", connection.Host, instancesResult.Error);
                continue;
            }

            foreach (var instance in instancesResult.Value.Instances)
            {
                if (string.IsNullOrEmpty(instance.Id))
                    continue;

                cancellationToken.ThrowIfCancellationRequested();

                var pushResult = await PushToInstance(connection, instance.Id, settings);
                if (pushResult.IsFailure)
                {
                    failed++;
                    errors.Add($"{connection.Host}/{instance.Id}: {pushResult.Error}");
                    _logger.LogWarning("Не удалось обновить настройки инстанса {InstanceId} на {Host}: {Error}",
                        instance.Id, connection.Host, pushResult.Error);
                    continue;
                }

                if (pushResult.Value)
                    updated++;
            }
        }

        return Result.Success(new PiotSettingsPushResult
        {
            UpdatedInstances = updated,
            FailedInstances = failed,
            Errors = errors
        });
    }

    private async Task<Result<bool>> PushToInstance(
        TsPiotConnectionSettings connection,
        string instanceId,
        PiotDeviceSettings settings)
    {
        var settingsResult = await _tsPiotEspApiService.InstanceSettings(connection, instanceId);
        if (settingsResult.IsFailure)
            return Result.Failure<bool>(settingsResult.Error);

        var current = settingsResult.Value;
        if (!ApplySettings(current, settings))
            return Result.Success(false);

        var updateResult = await _tsPiotEspApiService.UpdateInstanceSettings(connection, instanceId, current);
        if (updateResult.IsFailure)
            return Result.Failure<bool>(updateResult.Error);

        return Result.Success(true);
    }

    private static bool ApplySettings(TsPiotModuleSettings current, PiotDeviceSettings settings)
    {
        var needUpdate = false;

        if (settings.CdnCodesCheckTimeoutMs.HasValue
            && current.CdnCodesCheckTimeout != settings.CdnCodesCheckTimeoutMs.Value)
        {
            current.CdnCodesCheckTimeout = settings.CdnCodesCheckTimeoutMs.Value;
            needUpdate = true;
        }

        if (settings.CdnHealthCheckTimeoutMs.HasValue
            && current.CdnHealthCheckTimeout != settings.CdnHealthCheckTimeoutMs.Value)
        {
            current.CdnHealthCheckTimeout = settings.CdnHealthCheckTimeoutMs.Value;
            needUpdate = true;
        }

        if (settings.AllowRemoteConnection.HasValue
            && current.AllowRemoteConnection != settings.AllowRemoteConnection.Value)
        {
            current.AllowRemoteConnection = settings.AllowRemoteConnection.Value;
            needUpdate = true;
        }

        return needUpdate;
    }

    private static List<TsPiotConnectionSettings> DistinctConnections(List<PrintGroupData> printGroups)
    {
        var result = new List<TsPiotConnectionSettings>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var printGroup in printGroups)
        {
            var tsPiot = printGroup.TsPiot;
            if (string.IsNullOrEmpty(tsPiot.Host) || string.IsNullOrEmpty(tsPiot.Port) || tsPiot.InformationPort <= 0)
                continue;

            var key = $"{tsPiot.Host}:{tsPiot.InformationPort}";
            if (!seen.Add(key))
                continue;

            result.Add(tsPiot);
        }

        return result;
    }
}
