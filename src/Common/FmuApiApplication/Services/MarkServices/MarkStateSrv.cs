using CouchDb.Handlers;
using FmuApiDomain.MarkInformation;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Services.MarkServices
{
    public class MarkStateSrv
    {
        private readonly MarkInformationHandler _markStateCrud;
        private readonly ILogger<MarkStateSrv> _logger;

        public MarkStateSrv(MarkInformationHandler markStateCrud, ILogger<MarkStateSrv> logger)
        {
            _markStateCrud = markStateCrud;
            _logger = logger;
        }

        public async Task<MarkInformation> State(string sgtin)
        {
            return await _markStateCrud.GetAsync(sgtin);
        }

        public async Task SetMarksSaled(SaleMarkContract saleMarkData)
        {
            foreach (var mark in saleMarkData.Marks)
            {
                await _markStateCrud.SetStateAsync(mark, MarkState.Sold, saleMarkData.CheqData);
            }
        }

        public async Task SetMarksInStok(SaleMarkContract saleMarkData)
        {
            foreach (var mark in saleMarkData.Marks)
            {
                await _markStateCrud.SetStateAsync(mark, MarkState.Stock, saleMarkData.CheqData);
            }
        }
    }
}
