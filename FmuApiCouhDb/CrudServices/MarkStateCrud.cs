using FmuApiCouhDb;
using FmuApiCouhDb.DocumentModels;
using FmuApiDomain.Models.MarkState;

namespace FmuApiCouhDb.CrudServices
{
    public class MarkStateCrud
    {
        private CouchDbContext? _context;

        public MarkStateCrud() { }
        public MarkStateCrud(CouchDbContext couchDbContext)
        {
            _context = couchDbContext;
        }

        public async Task<MarkState> SetStateAsync(string id, string state, SaleData saleData)
        {
            if (_context == null)
                return new();

            var document = await GetDocumentAsync(id);

            if (document.Id == "")
                return new();

            document.State = state;
            document.TrueApiInformation.Sold = (state == "sold");

            MarkState documentData = new()
            {
                MarkId = document.Id,
                State = document.State,
                TrueApiCisData = document.TrueApiInformation,
                TrueApiAnswerProperties = document.TrueApiAnswerProperties,
                SaleData = saleData
            };

            return await AddAsync(documentData);
        }

        public async Task<MarkState> AddAsync(MarkState markState)
        {
            if (_context == null)
                return new();

            MarkStateDocument document = new()
            {
                Id = markState.TrueApiCisData.Cis,
                State = markState.State,
                TrueApiInformation = markState.TrueApiCisData,
                TrueApiAnswerProperties = markState.TrueApiAnswerProperties,
                SaleInforamtion = markState.SaleData
            };

            var dataRecord = await GetDocumentAsync(markState.TrueApiCisData.Cis);

            if (dataRecord.Rev != "")
            {
                document.Rev = dataRecord.Rev;
            }

            await _context.MarkState.AddOrUpdateAsync(document);

            dataRecord = await GetDocumentAsync(markState.TrueApiCisData.Cis);

            MarkState answer = new()
            {
                MarkId = dataRecord.Id,
                State = dataRecord.State,
                TrueApiCisData = dataRecord.TrueApiInformation,
                TrueApiAnswerProperties = dataRecord.TrueApiAnswerProperties,
                SaleData = dataRecord.SaleInforamtion
            };

            return answer;
        }

        public async Task<MarkState> GetAsync(string Id)
        {
            if (_context == null)
                return new();

            var dataRecord = await GetDocumentAsync(Id);

            return new()
            {
                MarkId = dataRecord.Id,
                State = dataRecord.State,
                TrueApiCisData = dataRecord.TrueApiInformation,
                TrueApiAnswerProperties = dataRecord.TrueApiAnswerProperties,
                SaleData = dataRecord.SaleInforamtion
            };
        }

        public async Task DelteAsync(string Id)
        {
            if (_context == null)
                return;

            var dataRecord = await GetDocumentAsync(Id);

            if (dataRecord.Id == "")
                return;

            await _context.MarkState.RemoveAsync(dataRecord);
        }

        public async Task BulkAddAsync(List<MarkState> markStates)
        {
            if (_context == null)
                return;

            List<MarkStateDocument> MarkStateDocumements = new();

            foreach (var markState in markStates)
            {
                MarkStateDocument markStateDocument = new()
                {
                    Id = markState.MarkId,
                    State = markState.State,
                    TrueApiInformation = markState.TrueApiCisData,
                    TrueApiAnswerProperties = markState.TrueApiAnswerProperties,
                    SaleInforamtion = markState.SaleData
                };

                MarkStateDocumements.Add(markStateDocument);
            }

            await _context.MarkState.AddOrUpdateRangeAsync(MarkStateDocumements);

        }

        public async Task BulkDeleteAsync(List<MarkState> markStates)
        {
            if (_context == null)
                return;

            List<MarkStateDocument> MarkStateDocumements = new();

            foreach (var markState in markStates)
            {
                MarkStateDocument markStateDocument = new()
                {
                    Id = markState.MarkId,
                    State = markState.State,
                    TrueApiInformation = markState.TrueApiCisData,
                    TrueApiAnswerProperties = markState.TrueApiAnswerProperties,
                    SaleInforamtion = markState.SaleData
                };

                MarkStateDocumements.Add(markStateDocument);
            }

            await _context.MarkState.DeleteRangeAsync(MarkStateDocumements);

        }

        private async Task<MarkStateDocument> GetDocumentAsync(string Id)
        {
            if (_context == null)
                return new();

            MarkStateDocument? answer = await _context.MarkState.FindAsync(Id);

            return answer ?? new();
        }
    }
}
