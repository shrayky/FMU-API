using CouchDb.DatabaseScheme;
using CouchDb.Queries;
using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Mark.Entities;
using FmuApiDomain.Mark.Enums;
using FmuApiDomain.Mark.Interfaces;
using FmuApiDomain.Mark.Models;
using FmuApiDomain.State.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sqlite.Documents;
using System.Text.Json;

namespace Sqlite.Repositories;

/// <summary>
/// Справочник марок в файле SQLite.
/// </summary>
public class MarkInformationRepository(
    ILogger<MarkInformationRepository> logger,
    SqliteDbContext context,
    IParametersService appConfiguration,
    IApplicationState applicationState)
    : BaseSqliteRepository<MarkEntity, MarkRow>(logger, context, context.Marks, appConfiguration, applicationState), IMarkInformationRepository
{
    public override string DatabaseName => DatabaseNames.MarksDbName;

    public async Task<MarkEntity> GetAsync(string id)
    {
        return await GetById(id) ?? new();
    }

    public async Task<MarkEntity> SetStateAsync(string id, string state, SaleData saleData)
    {
        var document = await GetAsync(id);

        document.Id = id;
        document.State = state;
        document.SaleData = saleData;
        document.TrueApiCisData.Sold = (state == MarkState.Sold);

        await Save(document);

        return document;
    }

    public async Task<MarkEntity> AddAsync(MarkEntity mark)
    {
        if (mark.Id == string.Empty)
            mark.Id = mark.MarkId;

        await Save(mark);

        return mark;
    }

    public async Task<List<MarkEntity>> GetDocumentsAsync(List<string> gtins)
    {
        return await GetListById(gtins);
    }

    public async Task<bool> AddRangeAsync(List<MarkEntity> markEntities)
    {
        return await SaveBulk(markEntities);
    }

    public async Task<Result<MarkSearchResult>> SearchMarkData(string searchTerm, int page, int pageSize)
    {
        if (!IsOnline())
            return new MarkSearchResult();

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            var totalCount = await CountDocuments();
            if (totalCount == null)
                return new MarkSearchResult();

            return await AllMarksWithPagination(page, pageSize, totalCount.Value);
        }

        return await SearchMarksWithPagination(searchTerm.Trim(), page, pageSize);
    }

    protected override MarkRow CreateRow(MarkEntity entity, string id) => new()
    {
        Id = id,
        Json = JsonSerializer.Serialize(entity, JsonOptions),
        MarkId = entity.MarkId,
        ReqTimestamp = entity.TrueApiAnswerProperties.ReqTimestamp
    };

    private async Task<int?> CountDocuments()
    {
        return await ExecuteSafetyDbOperation<int?>(
            async () => await _rows.CountAsync(),
            "GetDocumentsCount",
            null);
    }

    private async Task<Result<MarkSearchResult>> SearchMarksWithPagination(string searchTerm, int page, int pageSize)
    {
        var rows = await ExecuteSafetyDbOperation(
            async () => await _rows
                .Where(row => row.MarkId.StartsWith(searchTerm))
                .OrderBy(row => row.MarkId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize + 1)
                .ToListAsync(),
            "SearchMarks",
            (List<MarkRow>?)null);

        if (rows == null)
            return Result.Failure<MarkSearchResult>("Ошибка запроса к БД");

        var (count, totalPages) = MarkMangoQueryBuilder.ResolveSearchPagination(page, pageSize, rows.Count);

        return Result.Success(new MarkSearchResult
        {
            Marks = rows.Take(pageSize).Select(ToMarkListItem).ToList(),
            Count = count,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            SearchTerm = searchTerm
        });
    }

    private async Task<Result<MarkSearchResult>> AllMarksWithPagination(int page, int pageSize, int totalCount)
    {
        var rows = await ExecuteSafetyDbOperation(
            async () => await _rows
                .OrderByDescending(row => row.ReqTimestamp)
                .ThenBy(row => row.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(),
            "ListMarks",
            (List<MarkRow>?)null);

        if (rows == null)
            return Result.Failure<MarkSearchResult>("Ошибка запроса к БД");

        return Result.Success(new MarkSearchResult
        {
            Marks = rows.Select(ToMarkListItem).ToList(),
            Count = totalCount,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            SearchTerm = string.Empty
        });
    }

    private MarkListItem ToMarkListItem(MarkRow row)
    {
        var entity = ToEntity(row) ?? new MarkEntity();

        return MarkListItem.FromEntity(entity);
    }
}
