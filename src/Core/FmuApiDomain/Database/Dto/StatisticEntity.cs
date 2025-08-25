using FmuApiDomain.Templates.Tables;

namespace FmuApiDomain.Database.Dto
{
    public class StatisticEntity : IHaveStringId
    {
        public string Id { get; set; } = string.Empty;
        public string SGtin { get; set; } = string.Empty;
        public DateTime checkDate { get; set; } = DateTime.MinValue;
        public bool SuccessCheck { get; set; } = false;
        public bool OnLineCheck { get; set; } = false;
        public string WarningMessage { get; set; } = string.Empty;
    }
}
