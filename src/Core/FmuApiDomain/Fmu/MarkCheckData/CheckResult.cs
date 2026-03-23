using System.Text.Json.Serialization;

namespace FmuApiDomain.Fmu.MarkCheckData;

public class CheckResult
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Inn { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Kpp { get; set; }
    
    public CheckInformation Response { get; set; } = new();
}
