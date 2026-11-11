using CSharpFunctionalExtensions;
using CouchDb.Queries;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Models;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Logging;

namespace CouchDb.Repositories;

public class GisMtDocumentRepository : BaseCouchDbRepository<GisMtDocumentEntity>, IGisMtDocumentRepository
{
    public GisMtDocumentRepository(
        ILogger<GisMtDocumentRepository> logger,
        CouchDbContext context,
        IParametersService appConfiguration,
        IApplicationState applicationState)
        : base(logger, context, context.GisMtDocuments, appConfiguration, applicationState)
    {
    }

    /// <summary>
    /// Возвращает документ ГИС МТ по идентификатору.
    /// </summary>
    public async Task<GisMtDocumentEntity?> Get(string id)
    {
        if (_context == null)
            return null;

        return await GetByIdAsync(id);
    }

    /// <summary>
    /// Проверяет, был ли документ уже загружен.
    /// </summary>
    public async Task<bool> Exists(string id)
    {
        if (_context == null)
            return false;

        var entity = await GetByIdAsync(id);
        return entity != null && !string.IsNullOrEmpty(entity.Id);
    }

    /// <summary>
    /// Сохраняет факт загрузки документа.
    /// </summary>
    public async Task<bool> Save(GisMtDocumentEntity entity)
    {
        if (_context == null)
            return false;

        if (string.IsNullOrEmpty(entity.Id))
            entity.Id = entity.Number;

        var existing = await GetByIdAsync(entity.Id);
        if (existing == null)
            return await CreateAsync(entity);

        return await UpdateAsync(entity.Id, entity);
    }

    /// <summary>
    /// Страница загруженных входящих документов по убыванию даты загрузки.
    /// </summary>
    public async Task<Result<GisMtDocumentPage>> List(int page, int pageSize)
    {
        if (_context == null)
            return Result.Failure<GisMtDocumentPage>(DatabaseUnavailable);

        var documents = await ExecuteMangoQueryAsync(
            GisMtDocumentsMangoQueryBuilder.BuildPageQuery(page, pageSize));

        if (documents.IsFailure)
            return Result.Failure<GisMtDocumentPage>(documents.Error);

        var totalCount = await GetDocumentsCountAsync();

        if (totalCount == null)
            return Result.Failure<GisMtDocumentPage>("Не удалось получить число документов");

        foreach (var document in documents.Value)
            document.Body = string.Empty;

        return Result.Success(new GisMtDocumentPage
        {
            Documents = documents.Value,
            Count = totalCount.Value,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = totalCount.Value == 0 ? 1 : (int)Math.Ceiling((double)totalCount.Value / pageSize)
        });
    }
}
