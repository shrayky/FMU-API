namespace FmuApiDomain.TsPiot.Models;

/// <summary>
/// Результат отправки настроек на устройства ПИоТ.
/// </summary>
public record PiotSettingsPushResult
{
    public int UpdatedInstances { get; init; }

    public int FailedInstances { get; init; }

    public List<string> Errors { get; init; } = [];
}
