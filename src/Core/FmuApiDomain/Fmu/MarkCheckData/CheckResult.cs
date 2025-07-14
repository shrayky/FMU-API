using System.Text.Json.Serialization;

namespace FmuApiDomain.Fmu.MarkCheckData
{
    public class CheckResult
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Inn { get; set; } = string.Empty;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Kpp { get; set; } = string.Empty;
        public CheckInformation Response { get; set; } = new();

    }
}
