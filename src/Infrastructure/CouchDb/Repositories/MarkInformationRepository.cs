using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.MarkInformation.Entities;
using FmuApiDomain.MarkInformation.Enums;
using FmuApiDomain.MarkInformation.Models;
using FmuApiDomain.Repositories;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Logging;

namespace CouchDb.Repositories
{
    public class MarkInformationRepository(ILogger<MarkInformationRepository> logger, CouchDbContext context,
                                           IParametersService appConfiguration, IApplicationState applicationState) : BaseCouchDbRepository<MarkEntity>(logger, context, context.Marks, appConfiguration, applicationState), IMarkInformationRepository
    {
        public async Task<MarkEntity> GetAsync(string id)
        {
            if (_context == null)
                return new();

            var data = await base.GetByIdAsync(id);

            return data ?? new();
        }

        public async Task<MarkEntity> SetStateAsync(string id, string state, SaleData saleData)
        {
            if (_context == null)
                return new();

            var document = await GetAsync(id);

            document.State = state;
            document.SaleData = saleData;
            document.TrueApiCisData.Sold = (state == MarkState.Sold);

            await base.UpdateAsync(id, document);

            return document;
        }

        public async Task<MarkEntity> AddAsync(MarkEntity mark)
        {
            if (_context == null)
                return new();

            if (mark.Id == string.Empty)
                mark.Id = mark.MarkId;

            await CreateAsync(mark);

            return mark;            
        }

        public async Task<List<MarkEntity>> GetDocumentsAsync(List<string> gtins)
        {
            if (_context == null)
                return new();

            var marks = await GetListByIdAsync(gtins);

            return marks;
        }

        public async Task<bool> AddRangeAsync(List<MarkEntity> markEntities)
        {
            if (_context == null)
                return false;

            return await CreateBulkAsync(markEntities);
        }
    }
}
