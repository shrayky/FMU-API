using FmuApiDomain.MarkInformation.Models;

namespace FmuApiDomain.DTO.FmuApiExchangeData.NodeInformation;

public record CheckMarkStatisticInformation
{
    public long Date {  get; set; }
    public MarkCheckStatistics MarkCheckStatistics { get; set; } = new();
}
