using CouchDb.Documents;
using CouchDB.Driver;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.MarkInformation.Entities;
using FmuApiDomain.State.Interfaces;
using FmuApiDomain.Templates.Tables;
using Microsoft.Extensions.Logging;

namespace CouchDb.Repositories
{
    public class BaseCouchDbRepository<T> where T : class, IHaveStringId
    {
        protected readonly ILogger _logger;
        protected readonly CouchDbContext _context;
        protected readonly ICouchDatabase<CouchDoc<T>> _database;
        private readonly IParametersService _appConfiguration;
        protected readonly IApplicationState _appState;

        protected BaseCouchDbRepository(ILogger logger,
            CouchDbContext context,
            ICouchDatabase<CouchDoc<T>> database,
            IParametersService appConfiguration,
            IApplicationState applicationState)
        {
            _logger = logger;
            _context = context;
            _database = database;
            _appConfiguration = appConfiguration;
            _appState = applicationState;
        }

        public virtual async Task<T?> GetByIdAsync(string id)
        {
            var doc = await _database.FindAsync(id);

            return doc?.ToDomain();
        }

        public virtual async Task<bool> CreateAsync(T entity)
        {
            if (string.IsNullOrEmpty(entity.Id))
            {
                entity.Id = Guid.NewGuid().ToString();
            }

            return await SaveDocumentAsync(entity);
        }

        public virtual async Task<bool> UpdateAsync(string id, T entity)
        {
            entity.Id = id;
            return await SaveDocumentAsync(entity);
        }

        private async Task<bool> SaveDocumentAsync(T entity)
        {
            var existingDoc = await _database.FindAsync(entity.Id);
            var doc = CouchDoc<T>.FromDomain(entity, entity.Id);

            if (existingDoc != null)
            {
                doc.Rev = existingDoc.Rev;
            }

            await _database.AddOrUpdateAsync(doc);
            return true;
        }

        public virtual async Task<bool> DeleteAsync(string id)
        {
            if (_context == null)
                return false;

            var doc = await _database.FindAsync(id);

            if (doc == null)
                return true;

            if (doc.Id == "")
                return false;

            await _database.RemoveAsync(doc);

            return true;
        }

        public virtual async Task<bool> CreateBulkAsync(IEnumerable<T> entities)
        {
            var configuration = await _appConfiguration.CurrentAsync();
            int BATCH_SIZE = configuration.Database.BulkBatchSize;
            int MAX_PARALLEL_TASKS = configuration.Database.BulkParallelTasks;

            var ids = entities.Select(e => e.Id).ToList();
            var existingDocs = await _database.FindManyAsync(ids);

            var documentBatches = entities
               .Join(
                   existingDocs,
                   entity => entity.Id,
                   doc => doc.Id,
                   (entity, existingDoc) =>
                   {
                       var doc = CouchDoc<T>.FromDomain(entity, entity.Id);
                       doc.Rev = existingDoc.Rev;
                       return doc;
                   })
               .Union(entities
                   .Where(entity => !existingDocs.Any(doc => doc.Id == entity.Id))
                   .Select(entity => CouchDoc<T>.FromDomain(entity, entity.Id)))
               .GroupBy(e => e.Id)
               .Select(g => g.Last())
               .Chunk(BATCH_SIZE);

            var dbName = typeof(T).Name.ToLower();
            
            _logger.LogInformation("Начинаю массовое добавление в {Database}: {Count} документов", dbName, entities.Count());
            
            using var semaphore = new SemaphoreSlim(MAX_PARALLEL_TASKS);

            var tasks = documentBatches.Select(async batch =>
            {
                await semaphore.WaitAsync();
                try
                {
                    await _database.AddOrUpdateRangeAsync(batch);
                    await Task.Delay(100);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            return true;
        }

        public async Task<List<T>> GetListByIdAsync(List<string> ids)
        {
            var docs = await _database.FindManyAsync(ids);

            List<T> entityData = [];

            foreach (CouchDoc<T> couchDoc in docs)
            {
                entityData.Add(couchDoc.Data);
            }

            return entityData;
        }
    }
}
