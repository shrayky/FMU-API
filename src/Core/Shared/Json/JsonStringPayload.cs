using System.Text.Json;

namespace Shared.Json;

/// <summary>
/// Снимает JSON-обёртку строки из ответа ASP.NET Ok(string).
/// </summary>
public static class JsonStringPayload
{
    public static string Unwrap(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return raw;

        var trimmed = raw.Trim();
        if (trimmed.Length < 2 || trimmed[0] != '"' || trimmed[^1] != '"')
            return trimmed;

        try
        {
            var unwrapped = JsonSerializer.Deserialize<string>(trimmed);
            return string.IsNullOrEmpty(unwrapped) ? trimmed : unwrapped;
        }
        catch (JsonException)
        {
            return trimmed;
        }
    }
}
