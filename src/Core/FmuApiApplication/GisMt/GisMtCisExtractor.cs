using System.Text.Json;
using System.Text.RegularExpressions;
using FmuApiDomain.GisMt.Models;

namespace FmuApiApplication.GisMt;

/// <summary>
/// Извлекает коды маркировки из тела документа ГИС МТ.
/// </summary>
public static class GisMtCisExtractor
{
    private static readonly Regex CisLikeRegex = new(
        @"^[0-9A-Za-z!""%&'()*+,\-./:;<=>?_]{18,74}$",
        RegexOptions.Compiled);

    private static readonly HashSet<string> CisPropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "cis", "cis_list", "cises", "cisesList", "uit_code", "uitCode", "ki", "km", "codes", "code"
    };

    /// <summary>
    /// Извлекает уникальные КИ из ответа doc/info.
    /// </summary>
    public static List<string> Extract(GisMtDocInfoResponse document)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);

        if (document.CisesList != null)
        {
            foreach (var cis in document.CisesList)
                TryAdd(result, cis);
        }

        if (document.Body.HasValue)
            CollectFromElement(document.Body.Value, result);

        if (!string.IsNullOrWhiteSpace(document.Content))
        {
            try
            {
                using var contentDoc = JsonDocument.Parse(document.Content);
                CollectFromElement(contentDoc.RootElement, result);
            }
            catch (JsonException)
            {
                // content может быть не JSON — игнорируем
            }
        }

        return result.ToList();
    }

    private static void CollectFromElement(JsonElement element, HashSet<string> result)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (CisPropertyNames.Contains(property.Name))
                        CollectValues(property.Value, result);
                    else
                        CollectFromElement(property.Value, result);
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    CollectFromElement(item, result);
                break;
        }
    }

    private static void CollectValues(JsonElement element, HashSet<string> result)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                TryAdd(result, element.GetString());
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    CollectValues(item, result);
                break;
            case JsonValueKind.Object:
                CollectFromElement(element, result);
                break;
        }
    }

    private static void TryAdd(HashSet<string> result, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        var normalized = value.Trim().Trim('(', ')');
        if (normalized.Length is < 18 or > 74)
            return;

        if (!CisLikeRegex.IsMatch(normalized))
            return;

        result.Add(normalized);
    }
}
