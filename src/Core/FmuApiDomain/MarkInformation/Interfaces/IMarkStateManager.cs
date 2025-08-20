using CSharpFunctionalExtensions;
using FmuApiDomain.MarkInformation.Entities;
using FmuApiDomain.MarkInformation.Models;
using FmuApiDomain.TrueApi.MarkData.Check;

namespace FmuApiDomain.MarkInformation.Interfaces
{
    public interface IMarkStateManager
    {
        Task<MarkEntity> Information(string sgtin);
        Task<List<MarkEntity>> InformationBulk(List<string> sGtins);
        Task<Result> Save(string sgtin, CheckMarksDataTrueApi trueMarkData);
        Task<MarkEntity> ChangeState(string sGtin, string newState, SaleData saleData);
    }
}
