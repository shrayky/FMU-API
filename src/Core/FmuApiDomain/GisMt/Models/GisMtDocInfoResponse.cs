using System.Text.Json;
using System.Text.Json.Serialization;

namespace FmuApiDomain.GisMt.Models;

public class GisMtDocInfoResponse
{
    [JsonPropertyName("number")]
    public string Number { get; set; } = string.Empty;

    [JsonPropertyName("docDate")]
    public DateTime? DocDate { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("senderInn")]
    public string? SenderInn { get; set; }

    [JsonPropertyName("receiverInn")]
    public string? ReceiverInn { get; set; }

    [JsonPropertyName("input")]
    public bool Input { get; set; }

    [JsonPropertyName("productGroup")]
    public JsonElement? ProductGroup { get; set; }

    [JsonPropertyName("body")]
    public JsonElement? Body { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("cisesList")]
    public List<string>? CisesList { get; set; }

    [JsonPropertyName("errors")]
    public List<string>? Errors { get; set; }

    /// <summary>
    /// Ошибки обработки документа; пустой список, если ГИС МТ вернула null или не вернула поле.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<string> ErrorMessages => Errors ?? [];

    /// <summary>
    /// Ответ doc/info как пришёл от True API: сохраняется в документе целиком.
    /// </summary>
    [JsonIgnore]
    public string RawJson { get; set; } = string.Empty;
}
