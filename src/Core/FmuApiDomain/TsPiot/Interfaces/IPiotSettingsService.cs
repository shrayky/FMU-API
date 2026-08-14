using CSharpFunctionalExtensions;
using FmuApiDomain.TsPiot.Models;

namespace FmuApiDomain.TsPiot.Interfaces;

/// <summary>
/// Отправка настроек на устройства ПИоТ конкретного производителя.
/// </summary>
public interface IPiotSettingsService
{
    Task<Result<PiotSettingsPushResult>> PushSettings(
        PiotDeviceSettings settings,
        CancellationToken cancellationToken);
}
