using CSharpFunctionalExtensions;
using FmuApiDomain.TsPiot.Models;

namespace FmuApiDomain.TsPiot.Interfaces;

/// <summary>
/// Сценарий отправки текущих настроек приложения на устройства всех производителей ПИоТ.
/// </summary>
public interface IPiotSettingsPushService
{
    Task<Result<PiotSettingsPushResult>> PushCurrentSettings(CancellationToken cancellationToken);
}
