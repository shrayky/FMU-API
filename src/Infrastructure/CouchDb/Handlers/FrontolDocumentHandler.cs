using CouchDb.DocumentModels;
using FmuApiDomain.Fmu.Document.Interface;

namespace CouchDb.Handlers
{
    public class FrontolDocumentHandler
    {
        private CouchDbContext? _context;

        public FrontolDocumentHandler() { }

        public FrontolDocumentHandler(CouchDbContext couchDbContext)
        {
            _context = couchDbContext;
        }

        public async Task<FrontolDocumentData> GetAsync(string uid)
        {
            if (_context == null)
                return new();

            var dataRecord = await GetDocumentAsync(uid);

            return dataRecord;
        }

        public async Task<FrontolDocumentData> AddAsync(IFrontolDocumentData data)
        {
            if (_context == null)
                return new();

            FrontolDocumentData documentData = new()
            {
                Document = data.Document,
                Id = data.Document.Uid
            };

            var existDoc = await GetDocumentAsync(data.Document.Uid);

            if (existDoc.Rev != null)
                documentData.Rev = existDoc.Rev;

            documentData = await _context.FrontolDocuments.AddOrUpdateAsync(documentData);

            return await GetDocumentAsync(documentData.Id);
        }

        public async Task DelteAsync(string uid)
        {
            if (_context == null)
                return;

            var dataRecord = await GetDocumentAsync(uid);

            if (dataRecord.Id == "")
                return;

            await _context.FrontolDocuments.RemoveAsync(dataRecord);
        }

        public async Task BulkAddAsync(List<FrontolDocumentData> documents)
        {
            if (_context == null)
                return;

            await _context.FrontolDocuments.AddOrUpdateRangeAsync(documents);

        }

        public async Task BulkDeleteAsync(List<FrontolDocumentData> documents)
        {
            if (_context == null)
                return;

            await _context.FrontolDocuments.DeleteRangeAsync(documents);

        }

        private async Task<FrontolDocumentData> GetDocumentAsync(string uid)
        {
            if (_context == null)
                return new();

            FrontolDocumentData? answer = await _context.FrontolDocuments.FindAsync(uid);

            return answer ?? new();
        }

    }
}
