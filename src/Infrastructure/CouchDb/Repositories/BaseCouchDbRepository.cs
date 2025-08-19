using CouchDb.Documents;
using CouchDB.Driver;
using Flurl.Http;
using FmuApiDomain.Configuration.Interfaces;
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
            return await ExecuteSafetyDbOperation(
                async () =>
                {
                    var doc = await _database.FindAsync(id);
                    return doc?.ToDomain();
                },
                "GetById",
                default);
        }

        public virtual async Task<bool> CreateAsync(T entity)
        {
            if (string.IsNullOrEmpty(entity.Id))
                entity.Id = Guid.NewGuid().ToString();

            return await SaveDocumentAsync(entity);
        }

        public virtual async Task<bool> UpdateAsync(string id, T entity)
        {
            entity.Id = id;
            return await SaveDocumentAsync(entity);
        }

        public virtual async Task<bool> DeleteAsync(string id)
        {
            var doc = await CouchDocGet(id);

            if (doc == null)
                return true;

            if (doc.Id == "")
                return false;

            return await ExecuteSafetyDbOperation(
                async () =>
                {
                    await _database.RemoveAsync(doc);
                    return true;
                },
                "Delete",
                false);
        }

        public virtual async Task<bool> CreateBulkAsync(IEnumerable<T> entities)
        {
            var configuration = await _appConfiguration.CurrentAsync();
            int BATCH_SIZE = configuration.Database.BulkBatchSize;
            int MAX_PARALLEL_TASKS = configuration.Database.BulkParallelTasks;

            return await ExecuteSafetyDbOperation(
                async () =>
                {
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
                },
                "CreateBulk",
                false);
        }

        public async Task<List<T>> GetListByIdAsync(List<string> ids)
        {
            return await ExecuteSafetyDbOperation(
                async () =>
                {
                    var docs = await _database.FindManyAsync(ids);

                    List<T> entityData = [];

                    foreach (CouchDoc<T> couchDoc in docs)
                    {
                        entityData.Add(couchDoc.Data);
                    }

                    return entityData;
                },
                "GetListById",
                new List<T>());
        }

        private async Task<CouchDoc<T>?> CouchDocGet(string id)
        {
            return await ExecuteSafetyDbOperation(
                async () => await _database.FindAsync(id),
                "CouchDocGet",
                null);
        }

        private async Task<bool> SaveDocumentAsync(T entity)
        {

            return await ExecuteSafetyDbOperation(
                async () =>
                {
                    var existingDoc = await _database.FindAsync(entity.Id);
                    var doc = CouchDoc<T>.FromDomain(entity, entity.Id);

                    if (existingDoc != null)
                        doc.Rev = existingDoc.Rev;

                    await _database.AddOrUpdateAsync(doc);
                    return true;
                },
                "SaveDocument",
                false);
        }

        private async Task<TResult> ExecuteSafetyDbOperation<TResult>(Func<Task<TResult>> operation, string operationName, TResult defaultValue)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении операции {OperationName} в базе данных {DatabaseName}",
                    operationName, typeof(T).Name);

                HandleConnectionError(ex, operationName);

                return defaultValue;
            }
        }

        private async Task<bool> ExecuteSafetyDbOperation(Func<Task> operation, string operationName)
        {
            try
            {
                await operation();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении операции {OperationName} в базе данных {DatabaseName}",
                    operationName, typeof(T).Name);

                HandleConnectionError(ex, operationName);

                return false;
            }
        }

        private void HandleConnectionError(Exception ex, string operationName)
        {
            if (!IsConnectionError(ex))
                return;

            _appState.UpdateCouchDbState(false);
        }

        private bool IsConnectionError(Exception ex)
        {
            return ex is HttpRequestException ||
                   ex is TaskCanceledException ||
                   ex is OperationCanceledException ||
                   ex is FlurlHttpException ||
                   ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("подключение не установлено", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("отверг запрос на подключение", StringComparison.OrdinalIgnoreCase);
        }
    }
}
