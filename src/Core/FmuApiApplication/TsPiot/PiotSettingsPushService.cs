using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.TsPiot.Interfaces;
using FmuApiDomain.TsPiot.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FmuApiApplication.TsPiot;

[AutoRegisterService(ServiceLifetime.Scoped)]
public class PiotSettingsPushService(
    IParametersService parametersService,
    IEnumerable<IPiotSettingsService> piotSettingsServices) : IPiotSettingsPushService
{
    public const string TsPiotDisabled = "Режим работы с ТС ПИоТ выключен";

    /// <summary>
    /// Собирает настройки из конфигурации и отправляет их всем зарегистрированным производителям ПИоТ.
    /// </summary>
    public async Task<Result<PiotSettingsPushResult>> PushCurrentSettings(CancellationToken cancellationToken)
    {
        var appSettings = await parametersService.CurrentAsync();
        if (!appSettings.ServerConfig.TsPiotEnabled)
            return Result.Failure<PiotSettingsPushResult>(TsPiotDisabled);

        var timeouts = appSettings.HttpRequestTimeouts;
        var deviceSettings = new PiotDeviceSettings
        {
            CdnCodesCheckTimeoutMs = timeouts.CheckMarkRequestTimeout * 1000,
            CdnHealthCheckTimeoutMs = timeouts.CdnRequestTimeout * 1000,
            AllowRemoteConnection = true
        };

        var updated = 0;
        var failed = 0;
        var errors = new List<string>();

        foreach (var service in piotSettingsServices)
        {
            var result = await service.PushSettings(deviceSettings, cancellationToken);
            if (result.IsFailure)
                return Result.Failure<PiotSettingsPushResult>(result.Error);

            updated += result.Value.UpdatedInstances;
            failed += result.Value.FailedInstances;
            errors.AddRange(result.Value.Errors);
        }

        return Result.Success(new PiotSettingsPushResult
        {
            UpdatedInstances = updated,
            FailedInstances = failed,
            Errors = errors
        });
    }
}
