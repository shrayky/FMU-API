using System.Text.Json.Serialization;

namespace FmuApiDomain.GisMt.Models;

/// <summary>
/// Статусы ответа проверки марки через True API для UI.
/// </summary>
public static class MarkCheckTrueApiStatuses
{
    public const string Ok = "ok";
    public const string Disabled = "disabled";
    public const string NoToken = "no_token";
    public const string Offline = "offline";
    public const string Error = "error";
}

/// <summary>
/// Результат запроса cises/info для экрана проверки марки.
/// </summary>
public class MarkCheckTrueApiResult
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = MarkCheckTrueApiStatuses.Error;

    [JsonPropertyName("reason")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Reason { get; set; }

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<CisInfoResponseItem>? Data { get; set; }

    /// <summary>
    /// Создаёт результат с указанным статусом и причиной без данных.
    /// </summary>
    public static MarkCheckTrueApiResult WithStatus(string status, string reason) => new()
    {
        Status = status,
        Reason = reason
    };

    /// <summary>
    /// Создаёт успешный результат с данными cises/info.
    /// </summary>
    public static MarkCheckTrueApiResult Success(List<CisInfoResponseItem> data) => new()
    {
        Status = MarkCheckTrueApiStatuses.Ok,
        Data = data
    };
}
