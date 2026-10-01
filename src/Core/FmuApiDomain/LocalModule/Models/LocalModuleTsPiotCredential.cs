namespace FmuApiDomain.LocalModule.Models;

/// <summary>
/// Токен, которым ТС ПИоТ инициализировал ЛМ. Рабочее состояние: в файл настроек не сохраняется.
/// </summary>
public record LocalModuleTsPiotCredential(
    string Token,
    DateTime? ExpiresAtUtc,
    string FiscalDriveNumber);
