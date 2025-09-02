using CSharpFunctionalExtensions;
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

        public async Task<Result<MarkSearchResult>> SearchMarkData(string searchTerm, int page, int pageSize)
        {
            if (_context == null)
                return new();

            if (!_appState.CouchDbOnline() && _appState.NeedRestartService())
                return new();

            var dbInfo = await _database.GetInfoAsync();
            var totalCount = dbInfo.DocCount;

            Result<MarkSearchResult> searchResult;
            
            if (string.IsNullOrEmpty(searchTerm))
                searchResult = await AllMarksWithPagination(page, pageSize, totalCount);
            else
                searchResult = await SearchMarksWithPagination(searchTerm, page, pageSize);

            if (searchResult.IsFailure)
                return Result.Failure<MarkSearchResult>(searchResult.Error);

            return searchResult;
        }

        private async Task<Result<MarkSearchResult>> SearchMarksWithPagination(string searchTerm, int page, int pageSize)
        {
            var searchQuery = new
            {
                selector = new Dictionary<string, object>
                {
                    ["data"] = new Dictionary<string, object> { ["$exists"] = true },
                    ["data.markId"] = new Dictionary<string, object> { ["$regex"] = searchTerm }
                },
                sort = new[] { new Dictionary<string, string> { ["data.trueApiAnswerProperties.reqTimestamp"] = "desc" } }
            };

            var allResults = await ExecuteMangoQueryAsync(searchQuery);

            if (allResults.IsFailure)
                return Result.Failure<MarkSearchResult>(allResults.Error);

            var paginatedResults = allResults.Value
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var answer = new MarkSearchResult
            {
                Marks = paginatedResults,
                Count = allResults.Value.Count,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)allResults.Value.Count / pageSize),
                SearchTerm = searchTerm
            };

            return Result.Success(answer);
        }

        private async Task<Result<MarkSearchResult>> AllMarksWithPagination(int page, int pageSize, int totalCount)
        {
            var mangoQuery = new
            {
                selector = new Dictionary<string, object>
                {
                    ["data"] = new Dictionary<string, object> { ["$exists"] = true }
                },
                sort = new[] { new Dictionary<string, string> { ["data.trueApiAnswerProperties.reqTimestamp"] = "desc" } },
                limit = pageSize,
                skip = (page - 1) * pageSize
            };

            var paginatedResults = await ExecuteMangoQueryAsync(mangoQuery);

            var answer = new MarkSearchResult
            {
                Marks = paginatedResults.Value,
                Count = totalCount,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                SearchTerm = string.Empty
            };

            return Result.Success(answer);
        }
    }
}
