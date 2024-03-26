using FmuApiCouhDb.CrudServices;
using FmuApiDomain.Models.MarkState;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Services.MarkStateSrv
{
    public class MarkStateSrv
    {
        private readonly MarkStateCrud _markStateCrud;
        private readonly ILogger<MarkStateSrv> _logger;

        public MarkStateSrv(MarkStateCrud markStateCrud, ILogger<MarkStateSrv> logger)
        {
            _markStateCrud = markStateCrud;
            _logger = logger;
        }

        public async Task SetMarksSaled(SaleMarkContract saleMarkData)
        {
            foreach (var mark in saleMarkData.Marks)
            {
                await _markStateCrud.SetStateAsync(mark, "sold", saleMarkData.CheqData);
            }
        }

        public async Task SetMarksInStok(SaleMarkContract saleMarkData)
        {
            foreach (var mark in saleMarkData.Marks)
            {
                await _markStateCrud.SetStateAsync(mark, "stock", saleMarkData.CheqData);
            }
        }
    }
}
