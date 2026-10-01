namespace FmuApiDomain.LocalModule.Models;

/// <summary>
/// Параметры авторизации запроса проверки КИ в локальном модуле.
/// </summary>
public record LocalModuleCheckAuthorization(string? Token, string? FiscalDriveNumber);
