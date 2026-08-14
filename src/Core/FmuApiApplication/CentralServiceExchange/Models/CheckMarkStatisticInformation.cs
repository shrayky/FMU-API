using FmuApiDomain.Mark.Models;

namespace FmuApiApplication.CentralServiceExchange.Models;

public record CheckMarkStatisticInformation
{
    public long Date { get; set; }
    public MarkCheckStatistics MarkCheckStatistics { get; set; } = new();
}
