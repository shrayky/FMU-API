using FmuApiCouhDb.DocumentModels;
using FmuApiDomain.Models.MarkInformation;

namespace FmuApiCouhDb.CrudServices
{
    public class MarkInformationHandler
    {
        private CouchDbContext? _context;

        public MarkInformationHandler() { }
        public MarkInformationHandler(CouchDbContext couchDbContext)
        {
            _context = couchDbContext;
        }

        public async Task<MarkInformation> SetStateAsync(string id, string state, SaleData saleData)
        {
            if (_context == null)
                return new();

            var document = await GetDocumentAsync(id);

            document.State = state;
            document.TrueApiInformation.Sold = state == MarkState.Sold;

            MarkInformation documentData = new()
            {
                MarkId = document.Id,
                State = document.State,
                TrueApiCisData = document.TrueApiInformation,
                TrueApiAnswerProperties = document.TrueApiAnswerProperties,
                SaleData = saleData
            };

            return await AddAsync(documentData);
        }

        public async Task<MarkInformation> AddAsync(MarkInformation markState)
        {
            if (_context == null)
                return new();

            MarkStateDocument document = new()
            {
                Id = markState.MarkId,
                State = markState.State,
                TrueApiInformation = markState.TrueApiCisData,
                TrueApiAnswerProperties = markState.TrueApiAnswerProperties,
                SaleInforamtion = markState.SaleData
            };

            var dataRecord = await GetDocumentAsync(markState.MarkId);

            if (dataRecord.Rev != "")
            {
                document.Rev = dataRecord.Rev;
            }

            await _context.MarksState.AddOrUpdateAsync(document);

            dataRecord = await GetDocumentAsync(markState.MarkId);

            MarkInformation answer = new()
            {
                MarkId = dataRecord.Id,
                State = dataRecord.State,
                TrueApiCisData = dataRecord.TrueApiInformation,
                TrueApiAnswerProperties = dataRecord.TrueApiAnswerProperties,
                SaleData = dataRecord.SaleInforamtion
            };

            return answer;
        }

        public async Task<MarkInformation> GetAsync(string Id)
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

            await _context.MarksState.RemoveAsync(dataRecord);
        }

        public async Task BulkAddAsync(List<MarkInformation> markStates)
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

            await _context.MarksState.AddOrUpdateRangeAsync(MarkStateDocumements);

        }

        public async Task BulkDeleteAsync(List<MarkInformation> markStates)
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

            await _context.MarksState.DeleteRangeAsync(MarkStateDocumements);

        }

        private async Task<MarkStateDocument> GetDocumentAsync(string Id)
        {
            if (_context == null)
                return new();

            MarkStateDocument? answer = await _context.MarksState.FindAsync(Id);

            return answer ?? new();
        }
    }
}
