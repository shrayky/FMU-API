using System.Text.Json.Serialization;

namespace FmuApiDomNewDirectory1ain.Configuration;

public class CentralServerConnectionProperties
{
    public bool Enabled { get; set; } = false;
    public string Address {  get; set; } = string.Empty;
    public string Token {  get; set; } = string.Empty;
    public int ExchangeRequestInterval { get; set; } = 60;
    public string Secret { get; set; } = string.Empty;
    public bool DownloadNewVersion { get; set; } = false;
    public List<ScheduleTime> SchedulerUpdateInstall { get; set; } = new();
}

public record ScheduleTime
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("beginTime")]

    public TimeOnly BeginTime { get; set; }

    [JsonPropertyName("endTime")]
    public TimeOnly EndTime { get; set; }
}