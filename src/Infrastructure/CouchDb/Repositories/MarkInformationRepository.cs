using CouchDb.Queries;
using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Mark.Entities;
using FmuApiDomain.Mark.Enums;
using FmuApiDomain.Mark.Models;
using FmuApiDomain.Mark.Interfaces;
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

            if (!_appState.CouchDbOnline())
                return new();

            Result<MarkSearchResult> searchResult;

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                var totalCount = await GetDocumentsCountAsync();
                if (totalCount == null)
                    return new();

                searchResult = await AllMarksWithPagination(page, pageSize, totalCount.Value);
            }
            else
            {
                searchResult = await SearchMarksWithPagination(searchTerm.Trim(), page, pageSize);
            }

            if (searchResult.IsFailure)
                return Result.Failure<MarkSearchResult>(searchResult.Error);

            return searchResult;
        }

        private async Task<Result<MarkSearchResult>> SearchMarksWithPagination(string searchTerm, int page, int pageSize)
        {
            var searchQuery = MarkMangoQueryBuilder.BuildPrefixSearchQuery(searchTerm, page, pageSize);
            var searchResult = await ExecuteMangoQueryAsync(searchQuery);

            if (searchResult.IsFailure)
                return Result.Failure<MarkSearchResult>(searchResult.Error);

            var (count, totalPages) = MarkMangoQueryBuilder.ResolveSearchPagination(page, pageSize, searchResult.Value.Count);
            var marks = searchResult.Value
                .Take(pageSize)
                .Select(MarkListItem.FromEntity)
                .ToList();

            return Result.Success(new MarkSearchResult
            {
                Marks = marks,
                Count = count,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                SearchTerm = searchTerm
            });
        }

        private async Task<Result<MarkSearchResult>> AllMarksWithPagination(int page, int pageSize, int totalCount)
        {
            var mangoQuery = MarkMangoQueryBuilder.BuildListQuery(page, pageSize);
            var paginatedResults = await ExecuteMangoQueryAsync(mangoQuery);

            if (paginatedResults.IsFailure)
                return Result.Failure<MarkSearchResult>(paginatedResults.Error);

            return Result.Success(new MarkSearchResult
            {
                Marks = paginatedResults.Value.Select(MarkListItem.FromEntity).ToList(),
                Count = totalCount,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                SearchTerm = string.Empty
            });
        }
    }
}
