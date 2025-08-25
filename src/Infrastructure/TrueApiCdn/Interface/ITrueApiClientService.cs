using CSharpFunctionalExtensions;
using FmuApiDomain.TrueApi.MarkData.Check;

namespace TrueApi.Interface
{
    public interface ITrueApiClientService
    {
        Task<Result> HaveActiveCdns();
        Task<Result<CheckMarksDataTrueApi>> MarksOnLineCheck(CheckMarksRequestData marksRequestData, string xApiKey, TimeSpan timeoutInSeconds);
    }
}
