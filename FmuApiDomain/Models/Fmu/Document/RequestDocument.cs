using System.Text.Json.Serialization;

namespace FmuApiDomain.Models.Fmu.Document
{
    public class RequestDocument
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Action { get; set; } = string.Empty;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Uid { get; set; } = string.Empty;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Type { get; set; } = string.Empty;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Pos { get; set; } = string.Empty;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Shift { get; set; } = string.Empty;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Number { get; set; } = string.Empty;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string User { get; set; } = string.Empty;
        public List<Position> Positions { get; set; } = new();

        public Dictionary<string, string> MarkDictionary()
        {
            Dictionary<string, string> mark = new();

            foreach (var positon in Positions)
            {
                foreach (var requestedMark in positon.Marking_codes)
                {
                    mark.Add(requestedMark, System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(requestedMark)));
                }
            }

            return mark;
        }

        public bool IsAlcoholCheck()
        {
            bool alcoholCheck = false;

            foreach (var position in Positions)
            {
                if (position.Stamps.Count > 0)
                {
                    alcoholCheck = true;
                    break;
                }
            }

            return alcoholCheck;
        }
    }
}
