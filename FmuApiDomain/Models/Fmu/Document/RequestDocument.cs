namespace FmuApiDomain.Models.Fmu.Document
{
    public class RequestDocument
    {
        public string Action { get; set; } = string.Empty;
        public string Uid { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Pos { get; set; } = string.Empty;
        public string Shift { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
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


    }
}
