namespace FmuApiDomain.Configuration.Options;

public record TrueApiIntegrationSettings
{

    public bool Enable { get; set; } = false;

    public string Password { get; set; } = string.Empty;
}
