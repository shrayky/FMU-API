using CouchDb.Documents;
using CouchDB.Driver;
using CouchDB.Driver.Types;
using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options;
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
        protected readonly IParametersService _appConfiguration;
        protected readonly IApplicationState _appState;

        protected readonly CouchDbConnection _configuration;

        protected const string DatabaseUnavailable = "БД недоступна сейчас";

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

            _configuration = _appConfiguration.Current().Database;
        }

        public virtual async Task<T?> GetByIdAsync(string id)
        {
            return await ExecuteSafetyDbOperation(
                async () =>
                {
                    var response = await _database.ReadItemAsync(id);
                    return response?.Document.ToDomain();
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
                    await _database.DeleteItemAsync(doc.Id, doc.Rev);
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
                    var entityList = entities
                        .GroupBy(e => e.Id)
                        .Select(g => g.Last())
                        .ToList();
                    var ids = entityList.Select(e => e.Id).ToList();
                    var existingDocs = await _database.ReadItemsAsync(ids);
                    var existingById = existingDocs
                        .GroupBy(doc => doc.Id)
                        .ToDictionary(g => g.Key, g => g.Last());

                    var documentBatches = entityList
                        .Select(entity =>
                        {
                            var doc = CouchDoc<T>.FromDomain(entity, entity.Id);
                            if (existingById.TryGetValue(entity.Id, out var existingDoc))
                                doc.Rev = existingDoc.Rev;
                            return doc;
                        })
                        .Chunk(BATCH_SIZE);

                    var dbName = typeof(T).Name.ToLower();

                    _logger.LogInformation("Начинаю массовое добавление в {Database}: {Count} документов", dbName, entityList.Count);

                    using var semaphore = new SemaphoreSlim(MAX_PARALLEL_TASKS);

                    var tasks = documentBatches.Select(async batch =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            var operations = batch
                                .Select(doc => string.IsNullOrEmpty(doc.Rev)
                                    ? BulkItemOperation.Add(doc)
                                    : BulkItemOperation.Update(doc, doc.Id, doc.Rev))
                                .ToList();

                            await _database.ExecuteBulkItemOperationsAsync(operations);
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
                    var docs = await _database.ReadItemsAsync(ids);
                    return docs.Select(couchDoc => couchDoc.Data).ToList();
                },
                "GetListById",
                new List<T>());
        }

        /// <summary>
        /// Число документов в базе без design-документов индексов.
        /// </summary>
        protected async Task<int?> GetDocumentsCountAsync()
        {
            return await ExecuteSafetyDbOperation<int?>(
                async () =>
                {
                    var dbInfo = await _database.GetInfoAsync();
                    var indexes = await _database.GetIndexesAsync();
                    var designDocs = indexes
                        .Where(index => !string.IsNullOrWhiteSpace(index.DesignDocument))
                        .Select(index => index.DesignDocument)
                        .Distinct(StringComparer.Ordinal)
                        .Count();

                    var count = (int)dbInfo.DocCount - designDocs;
                    return count < 0 ? 0 : count;
                },
                "GetDocumentsCount",
                null);
        }

        public async Task<Result<List<T>>> ExecuteMangoQueryAsync(object mangoQuery)
        {
            var data = await ExecuteSafetyDbOperation(
                async () =>
                {
                    var result = await _database.QueryAsync(mangoQuery, throwExceptionOnWarning: false);
                    return result.Select(p => p.Data).ToList();
                },
                "MangoQuery",
                (List<T>?)null);

            if (data == null)
                return Result.Failure<List<T>>("Ошибка запроса к БД");

            return Result.Success(data);
        }

        private async Task<CouchDoc<T>?> CouchDocGet(string id)
        {
            return await ExecuteSafetyDbOperation(
                async () =>
                {
                    var response = await _database.ReadItemAsync(id);
                    if (response == null)
                        return null;

                    var doc = response.Document;
                    // Rev из ответа ReadItem надёжнее, чем поле документа
                    if (string.IsNullOrEmpty(doc.Rev))
                        doc.Rev = response.Rev;

                    return doc;
                },
                "CouchDocGet",
                null);
        }

        private async Task<bool> SaveDocumentAsync(T entity)
        {
            return await ExecuteSafetyDbOperation(
                async () =>
                {
                    var existingResponse = await _database.ReadItemAsync(entity.Id);
                    var doc = CouchDoc<T>.FromDomain(entity, entity.Id);

                    if (existingResponse != null)
                        await _database.UpdateItemAsync(doc, entity.Id, existingResponse.Rev);
                    else
                        await _database.CreateItemAsync(doc);

                    return true;
                },
                "SaveDocument",
                false);
        }

        protected async Task<TResult> ExecuteSafetyDbOperation<TResult>(Func<Task<TResult>> operation, string operationName, TResult defaultValue)
        {
            if (!_configuration.Enable)
                return defaultValue;

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

        protected async Task<bool> ExecuteSafetyDbOperation(Func<Task> operation, string operationName)
        {
            if (!_configuration.Enable)
                return false;

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
                   ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("подключение не установлено", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("отверг запрос на подключение", StringComparison.OrdinalIgnoreCase);
        }
    }
}
