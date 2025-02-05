namespace FmuApiDomain.MarkInformation.Models
{
    public class CheckWithMarks
    {
        public SaleData CheckData { get; set; } = new();
        public List<string> Marks { get; set; } = [];
    }
}
