using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.Repositories;
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
}
