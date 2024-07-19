using FmuApiCouhDb.DocumentModels;

namespace FmuApiCouhDb.CrudServices
{
    public class FrontolDocumentCrud
    {
        private CouchDbContext? _context;

        public FrontolDocumentCrud() {}

        public FrontolDocumentCrud(CouchDbContext couchDbContext)
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

        public async Task<FrontolDocumentData> AddAsync(FrontolDocumentData data)
        {
            if (_context == null)
                return new();

            var existDoc = await GetDocumentAsync(data.Document.Uid);

            if (existDoc.Rev != null)
                data.Rev = existDoc.Rev;

            data = await _context.FrontolDocuments.AddOrUpdateAsync(data);

            return await GetDocumentAsync(data.Id);
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

            FrontolDocumentData ? answer = await _context.FrontolDocuments.FindAsync(uid);

            return answer ?? new();
        }

    }
}
