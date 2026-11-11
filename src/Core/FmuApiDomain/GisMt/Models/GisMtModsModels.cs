using System.Text.Json;
using System.Text.Json.Serialization;

namespace FmuApiDomain.GisMt.Models;

/// <summary>
/// Место осуществления деятельности организации в ГИС МТ.
/// </summary>
public class GisMtModInfo
{
    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("fiasId")]
    public string FiasId { get; set; } = string.Empty;

    [JsonPropertyName("kpp")]
    public string Kpp { get; set; } = string.Empty;
}

/// <summary>
/// Ответ mods/list: страница мест и признак следующей страницы.
/// </summary>
public class GisMtModsResponse
{
    [JsonPropertyName("mods")]
    public List<GisMtModInfo> Mods { get; set; } = [];

    [JsonPropertyName("nextPage")]
    public bool NextPage { get; set; }

    public bool IsEmpty() => Mods.Count == 0;

    public static GisMtModsResponse Parse(string? json)
    {
        var value = (json ?? string.Empty).Trim();

        if (value.Length == 0)
            return new GisMtModsResponse();

        using var document = JsonDocument.Parse(value);

        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            return new GisMtModsResponse
            {
                Mods = document.RootElement.Deserialize<List<GisMtModInfo>>() ?? []
            };
        }

        return document.RootElement.Deserialize<GisMtModsResponse>() ?? new GisMtModsResponse();
    }
}
