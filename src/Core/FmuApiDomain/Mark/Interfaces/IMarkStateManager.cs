using CSharpFunctionalExtensions;
using FmuApiDomain.Mark.Entities;
using FmuApiDomain.Mark.Models;
using FmuApiDomain.TrueApi.MarkData.Check;

namespace FmuApiDomain.Mark.Interfaces
{
    public interface IMarkStateManager
    {
        Task<MarkEntity> Information(string sGtin);
        
        Task<List<MarkEntity>> InformationBulk(List<string> sGtins);
        
        Task<Result> Save(string sGtin, CheckMarksDataTrueApi trueMarkData);
        
        Task<MarkEntity> ChangeState(string sGtin, string newState, SaleData saleData);

        Task<DateTime?> ExpireDateFromGisMtStock(string sGtin);
    }
}
