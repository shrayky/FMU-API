using FmuApiDomain.Templates.Tables;

namespace FmuApiDomain.Database.Dto;

public class BeerTapEntity : IHaveStringId
{
    public string Id { get; set; } = string.Empty;

    public string MarkCode { get; set; } = string.Empty;

    public int Volume { get; set; } = 0;

    public string WareName { get; set; } = string.Empty;
    public string WareCode { get; set; } = string.Empty;

    public long LastUpdate { get; set; } = 0;

    public string TapName { get; set; } = string.Empty;

    public int Sales {  get; set; } = 0;
}
