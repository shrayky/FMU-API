using CouchDb;
using CouchDb.DocumentModels;
using FmuApiDomain.MarkInformation.Entities;
using FmuApiDomain.MarkInformation.Enums;
using FmuApiDomain.MarkInformation.Models;
using System.Runtime.CompilerServices;

namespace CouchDb.Handlers
{
    public class MarkInformationHandler
    {
        private CouchDbContext? _context;

        public MarkInformationHandler() { }
        public MarkInformationHandler(CouchDbContext couchDbContext)
        {
            _context = couchDbContext;
        }

        public async Task<MarkEntity> SetStateAsync(string id, string state, SaleData saleData)
        {
            if (_context == null)
                return new();

            var document = await GetDocumentAsync(id);

            document.State = state;
            document.TrueApiInformation.Sold = state == MarkState.Sold;

            MarkEntity documentData = new()
            {
                MarkId = document.Id,
                State = document.State,
                TrueApiCisData = document.TrueApiInformation,
                TrueApiAnswerProperties = document.TrueApiAnswerProperties,
                SaleData = saleData
            };

            return await AddAsync(documentData);
        }

        public async Task<MarkEntity> AddAsync(MarkEntity markState)
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

            dataRecord.Rev = dataRecord.Rev ?? "";

            if (dataRecord.Rev != "")
            {
                document.Rev = dataRecord.Rev;
            }

            await _context.MarksState.AddOrUpdateAsync(document);

            dataRecord = await GetDocumentAsync(markState.MarkId);

            MarkEntity answer = new()
            {
                MarkId = dataRecord.Id,
                State = dataRecord.State,
                TrueApiCisData = dataRecord.TrueApiInformation,
                TrueApiAnswerProperties = dataRecord.TrueApiAnswerProperties,
                SaleData = dataRecord.SaleInforamtion
            };

            return answer;
        }

        public async Task<MarkEntity> GetAsync(string Id)
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

        public async Task DeleteAsync(string Id)
        {
            if (_context == null)
                return;

            var dataRecord = await GetDocumentAsync(Id);

            if (dataRecord.Id == "")
                return;

            await _context.MarksState.RemoveAsync(dataRecord);
        }

        public async Task BulkAddAsync(List<MarkEntity> markStates)
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

        public async Task BulkDeleteAsync(List<MarkEntity> markStates)
        {
            if (_context == null)
                return;

            List<MarkStateDocument> MarkStateDocuments = new();

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

                MarkStateDocuments.Add(markStateDocument);
            }

            await _context.MarksState.DeleteRangeAsync(MarkStateDocuments);

        }

        private async Task<MarkStateDocument> GetDocumentAsync(string Id)
        {
            if (_context == null)
                return new();

            MarkStateDocument? answer = await _context.MarksState.FindAsync(Id);

            return answer ?? new();
        }

        public async Task<List<MarkEntity>> GetDocumentsAsync(List<string> gtins)
        {
            if (_context == null)
                return new();

            var docs = await _context.MarksState.FindManyAsync(gtins);

            List<MarkEntity> answer = [];

            foreach (var doc in docs)
            {
                MarkEntity markEntity = new()
                {
                    MarkId = doc.Id,
                    State = doc.State,
                    TrueApiCisData = doc.TrueApiInformation,
                    TrueApiAnswerProperties = doc.TrueApiAnswerProperties,
                    SaleData = doc.SaleInforamtion
                };

                answer.Add(markEntity);
            }

            return answer;
        }
    }
}
