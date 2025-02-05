namespace FmuApiDomain.MarkInformation.Models
{
    public class SaleData
    {
        public string Pos { get; set; } = string.Empty;
        public string CheckNumber { get; set; } = "0";
        public DateTime SaleDate { get; set; } = DateTime.MinValue;
        public bool IsSale { get; set; } = true;
    }
}
