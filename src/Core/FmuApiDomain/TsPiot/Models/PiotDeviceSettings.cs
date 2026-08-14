namespace FmuApiDomain.TsPiot.Models;

/// <summary>
/// Настройки, которые отправляются на устройства ПИоТ.
/// Незаполненные поля не изменяются на устройстве.
/// </summary>
public record PiotDeviceSettings
{
    public int? CdnCodesCheckTimeoutMs { get; init; }

    public int? CdnHealthCheckTimeoutMs { get; init; }

    public bool? AllowRemoteConnection { get; init; }
}
