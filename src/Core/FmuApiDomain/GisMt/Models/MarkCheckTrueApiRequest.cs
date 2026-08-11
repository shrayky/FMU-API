using System.Text.Json.Serialization;

namespace FmuApiDomain.GisMt.Models;

/// <summary>
/// Запрос сведений о КИ через True API для экрана проверки марки.
/// </summary>
public class MarkCheckTrueApiRequest
{
    [JsonPropertyName("inn")]
    public string Inn { get; set; } = string.Empty;

    [JsonPropertyName("cises")]
    public List<string> Cises { get; set; } = [];
}
