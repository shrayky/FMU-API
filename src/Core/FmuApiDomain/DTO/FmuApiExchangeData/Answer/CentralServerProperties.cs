using System.Text.Json.Serialization;

namespace FmuApiDomain.DTO.FmuApiExchangeData.Answer;

public record CentralServerProperties
{
    [JsonPropertyName("exchangeServerAddresses")]
    public string ExchangeServerAddresses { get; init; } = string.Empty;

    [JsonPropertyName("exchangeRequestInterval")]
    public int ExchangeRequestInterval { get; init; }

    [JsonPropertyName("schedulerUpdateDownload")]
    public List<ScheduleTimeDto> SchedulerUpdateDownload { get; init; } = [];
}

public record ScheduleTimeDto
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("beginTime")]
    public TimeOnly BeginTime { get; init; }

    [JsonPropertyName("endTime")]
    public TimeOnly EndTime { get; init; }
}
