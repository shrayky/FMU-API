using CSharpFunctionalExtensions;
using FmuApiDomain.MarkInformation;
using FmuApiDomain.TrueSignApi.MarkData.Check;

namespace FmuApiApplication.Mark.Interfaces
{
    public interface IMarkStateManager
    {
        Task<MarkInformation> GetMarkInformation(string sgtin);
        Task<Result> SaveMarkInformation(string sgtin, CheckMarksDataTrueApi trueMarkData);
    }
}
