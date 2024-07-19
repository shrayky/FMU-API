namespace FmuApiDomain.Models.MarkInformation
{
    public class SaleData
    {
        public string Pos { get; set; } = string.Empty;
        public int CheqNumber { get; set; } = 0;
        public DateTime SaleDate { get; set; } = DateTime.MinValue;
        public bool IsSale { get; set; } = true;
    }
}
