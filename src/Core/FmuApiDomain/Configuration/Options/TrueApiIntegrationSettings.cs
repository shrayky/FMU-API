namespace FmuApiDomain.Configuration.Options;

public record TrueApiIntegrationSettings
{
    public bool Enable { get; set; } = false;

    public string Password { get; set; } = string.Empty;

    public string DigitalSignature { get; set; } = string.Empty;

    /// <summary>
    /// Подключённые товарные группы участника (кэш из /participants).
    /// </summary>
    public List<string> ProductGroups { get; set; } = [];
}
