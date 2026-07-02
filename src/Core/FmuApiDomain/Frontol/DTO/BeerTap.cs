
namespace FmuApiDomain.Frontol.DTO;

public class BeerTap
{
    public string TapName { get; set; } = string.Empty;

    public int TapCode { get; set; } = 0;

    public string MarkCode { get; set; } = string.Empty;

    public double Volume { get; set; }

    public int WareId { get; set; } = 0;

    public int WareCode { get; set; } = 0;

    public string WareArticle { get; set; } = string.Empty;
}
