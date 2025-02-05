using CSharpFunctionalExtensions;
using FmuApiDomain.TrueApi.MarkData.Check;

namespace FmuApiApplication.Mark.Interfaces
{
    public interface IMarkStateManager
    {
        Task<FmuApiDomain.MarkInformation.Entities.MarkEntity> GetMarkInformation(string sgtin);
        Task<Result> SaveMarkInformation(string sgtin, CheckMarksDataTrueApi trueMarkData);
    }
}
