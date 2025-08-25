using CSharpFunctionalExtensions;
using FmuApiDomain.TrueApi.MarkData.Check;

namespace FmuApiDomain.TrueApi.Interfaces
{
    public interface IOnLineMarkCheckService
    {
        Task<Result<CheckMarksDataTrueApi>> RequestMarkState(CheckMarksRequestData marks, int organizationCode);
        Task<Result<CheckMarksDataTrueApi>> RequestMarkState(CheckMarksRequestData marks);
    }
}
