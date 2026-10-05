using System.Text.Json.Serialization;

namespace FmuApiDomain.Configuration.Options;

public record ScheduleTime
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("beginTime")]
    public TimeOnly BeginTime { get; set; }

    [JsonPropertyName("endTime")]
    public TimeOnly EndTime { get; set; }
}
