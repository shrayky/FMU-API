using FmuApiDomain.Configuration.Options.Organization;

namespace FmuApiDomain.TrueApiIntegration;

// При истёкшем x-api-key в проверку, CDN и инициализацию ЛМ уходит токен True API, сохранённая строка ключа не отправляется.
public static class XApiKeyCredential
{
    public static string Resolve(PrintGroupData? group, string? trueApiToken)
    {
        if (group == null)
            return string.Empty;

        if (!group.XApiKeyExpired)
            return group.XAPIKEY ?? string.Empty;

        return (trueApiToken ?? string.Empty).Trim();
    }
}
