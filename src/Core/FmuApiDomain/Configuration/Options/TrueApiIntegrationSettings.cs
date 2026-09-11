namespace FmuApiDomain.Configuration.Options;

/// <summary>
/// Настройки интеграции организации с True API ГИС МТ.
/// </summary>
public record TrueApiIntegrationSettings
{
    public bool Enable { get; set; } = false;

    public bool UseExternalToken { get; set; } = false;

    public string Password { get; set; } = string.Empty;

    public string DigitalSignature { get; set; } = string.Empty;

    public List<string> ProductGroups { get; set; } = [];

    public bool ShouldRequestTokenViaCryptoPro() => Enable && !UseExternalToken;
}
