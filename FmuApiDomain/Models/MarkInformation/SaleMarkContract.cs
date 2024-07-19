using FmuApiDomain.Models.TrueSignApi;

namespace FmuApiDomain.Models.MarkInformation
{
    public class SaleMarkContract
    {
        public SaleData CheqData { get; set; } = new();
        public List<string> Marks { get; set; } = new();
    }
}
