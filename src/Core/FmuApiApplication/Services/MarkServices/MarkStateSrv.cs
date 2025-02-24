using CouchDb.Handlers;
using FmuApiDomain.MarkInformation.Enums;
using FmuApiDomain.MarkInformation.Models;
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

        public async Task<FmuApiDomain.MarkInformation.Entities.MarkEntity> State(string sgtin)
        {
            return await _markStateCrud.GetAsync(sgtin);
        }

        public async Task SetMarksSold(CheckWithMarks saleMarkData)
        {
            foreach (var mark in saleMarkData.Marks)
            {
                await _markStateCrud.SetStateAsync(mark, MarkState.Sold, saleMarkData.CheckData);
            }
        }

        public async Task SetMarksInStok(CheckWithMarks saleMarkData)
        {
            foreach (var mark in saleMarkData.Marks)
            {
                await _markStateCrud.SetStateAsync(mark, MarkState.Stock, saleMarkData.CheckData);
            }
        }
    }
}
