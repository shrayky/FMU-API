using FmuApiDomain.Configuration.Options.Organization;

namespace FmuApiDomain.TrueApiIntegration;

/// <summary>
/// Выбирает учётные данные для проверки марок, CDN и инициализации ЛМ.
/// Живой x-api-key отправляется как есть. Пустой или истёкший ключ заменяется токеном True API.
/// </summary>
public static class XApiKeyCredential
{
    public static string Resolve(PrintGroupData? group, string? trueApiToken)
    {
        if (group == null)
            return string.Empty;

        var key = (group.XAPIKEY ?? string.Empty).Trim();
        if (!group.XApiKeyExpired && key.Length > 0)
            return key;

        return (trueApiToken ?? string.Empty).Trim();
    }
}
