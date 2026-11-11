using CouchDb.DatabaseScheme;
using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Models;
using FmuApiDomain.State.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sqlite.Documents;
using System.Text.Json;

namespace Sqlite.Repositories;

/// <summary>
/// Загруженные документы ГИС МТ в файле SQLite.
/// </summary>
public class GisMtDocumentRepository(
    ILogger<GisMtDocumentRepository> logger,
    SqliteDbContext context,
    IParametersService appConfiguration,
    IApplicationState applicationState)
    : BaseSqliteRepository<GisMtDocumentEntity, GisMtDocumentRow>(logger, context, context.GisMtDocuments, appConfiguration, applicationState), IGisMtDocumentRepository
{
    public override string DatabaseName => DatabaseNames.GisMtDocumentsDbName;

    /// <summary>
    /// Возвращает документ ГИС МТ по идентификатору.
    /// </summary>
    public async Task<GisMtDocumentEntity?> Get(string id)
    {
        return await GetById(id);
    }

    /// <summary>
    /// Проверяет, был ли документ уже загружен.
    /// </summary>
    public async Task<bool> Exists(string id)
    {
        var entity = await GetById(id);

        return entity != null && !string.IsNullOrEmpty(entity.Id);
    }

    /// <summary>
    /// Сохраняет факт загрузки документа.
    /// </summary>
    public override async Task<bool> Save(GisMtDocumentEntity entity)
    {
        if (string.IsNullOrEmpty(entity.Id))
            entity.Id = entity.Number;

        return await base.Save(entity);
    }

    /// <summary>
    /// Страница загруженных входящих документов по убыванию даты загрузки.
    /// </summary>
    public async Task<Result<GisMtDocumentPage>> List(int page, int pageSize)
    {
        return await ExecuteSafetyDbOperation(
            async () =>
            {
                var query = _rows.AsNoTracking();
                var count = await query.CountAsync();

                var rows = await query
                    .OrderByDescending(row => row.LoadedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var documents = rows
                    .Select(ToEntity)
                    .Where(document => document != null)
                    .Cast<GisMtDocumentEntity>()
                    .ToList();

                foreach (var document in documents)
                    document.Body = string.Empty;

                return Result.Success(new GisMtDocumentPage
                {
                    Documents = documents,
                    Count = count,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalPages = count == 0 ? 1 : (int)Math.Ceiling(count / (double)pageSize)
                });
            },
            "List",
            Result.Failure<GisMtDocumentPage>(DatabaseUnavailable));
    }

    protected override GisMtDocumentRow CreateRow(GisMtDocumentEntity entity, string id) => new()
    {
        Id = id,
        Json = JsonSerializer.Serialize(entity, JsonOptions),
        Number = entity.Number,
        LoadedAt = entity.LoadedAt
    };
}
