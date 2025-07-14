using FmuApiDomain.TrueApi.MarkData;
using FmuApiDomain.TrueApi.MarkData.Check;

namespace FmuApiDomain.Fmu.MarkCheckData
{
    public class CheckInformation
    {
        public int Code { get; set; } = 0;
        public List<CodeDataTrueApi> Codes { get; set; } = [];
    }
}
