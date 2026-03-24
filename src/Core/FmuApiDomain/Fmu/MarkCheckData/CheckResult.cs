using System.Text.Json.Serialization;

namespace FmuApiDomain.Fmu.MarkCheckData;

public class CheckResult
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Inn { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Kpp { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public CheckMarkResults? Response { get; set; }
}

public record CheckMarkResults
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<CheckInformation>? Results { get; set;}
}
