using System.Text.Json.Serialization;

namespace FmuApiDomain.Fmu.Document
{
    public class Position
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public List<string> Stamps { get; set; } = [];
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public List<string> Marking_codes { get; set; } = [];
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public double Total_price { get; set; } = 0;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Id { get; set; } = string.Empty;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Text { get; set; } = string.Empty;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public double Quantity { get; set; } = 0;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public double Volume { get; set; } = 0;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Organization Organization { get; set; } = new();
    }
}
