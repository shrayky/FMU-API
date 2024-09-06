using FmuApiDomain.Models.Configuration.TrueSign;
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
        public int Number { get; set; } = 0;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string User { get; set; } = string.Empty;
        public List<Position> Positions { get; set; } = new();

        public Dictionary<string, string> MarkDictionary()
        {
            Dictionary<string, string> mark = [];

            foreach (var positon in Positions)
            {
                foreach (var requestedMark in positon.Marking_codes)
                {
                    mark.Add(requestedMark, System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(requestedMark)));
                }
            }

            return mark;
        }

        public List<string> DecodedMarksList()
        {
            List<string> marks = [];

            foreach (var positon in Positions)
            {
                foreach (var requestedMark in positon.Marking_codes)
                {
                    marks.Add(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(requestedMark)));
                }
            }

            return marks;
        }

        public int MarksCount()
        {
            int count = 0;

            foreach (var positon in Positions)
                count += positon.Marking_codes.Count;

            return count;
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

        public string Mark()
        {
            if (Positions.Count != 1)
                return "";

            if (Positions[0].Marking_codes.Count != 1)
                return "";

            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(Positions[0].Marking_codes[0]));

        }
    }
}
