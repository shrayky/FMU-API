using System.Text.Json.Serialization;

namespace FmuApiDomain.Fmu.MarkCheckData;

public class OnlineCheckResult
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Inn { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Kpp { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public CheckInformation? Response { get; set; }
}
