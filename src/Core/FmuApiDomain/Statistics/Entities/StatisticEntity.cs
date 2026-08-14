using FmuApiDomain.Documents;
using FmuApiDomain.Templates.Tables;

namespace FmuApiDomain.Statistics.Entities;

public class StatisticEntity : IHaveStringId
{
    public string Id { get; set; } = string.Empty;
    public string SGtin { get; set; } = string.Empty;
    public DateTime CheckDate { get; set; } = DateTime.MinValue;
    public long CheckDay { get; set; } = 0;
    public bool SuccessCheck { get; set; } = false;
    public bool OnLineCheck { get; set; } = false;
    public bool OffLineCheck { get; set; } = false;
    public string WarningMessage { get; set; } = string.Empty;
    public RequestDocument? CheckRequest { get; set; }
    public FmuAnswer? CheckResponse { get; set; }
}
