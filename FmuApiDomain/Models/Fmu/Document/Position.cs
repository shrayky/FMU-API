namespace FmuApiDomain.Models.Fmu.Document
{
    public class Position
    {
        public List<string> Stamps { get; set; } = [];
        public List<string> Marking_codes { get; set; } = [];
        public double Total_price { get; set; } = 0;
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public double Quantity { get; set; } = 0;
        public Organization Organization { get; set; } = new();
    }
}
