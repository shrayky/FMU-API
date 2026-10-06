using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.State.Interfaces;
using FmuApiDomain.Templates.Tables;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sqlite.Documents;
using System.Text.Json;

namespace Sqlite.Repositories;

/// <summary>
/// Общая часть репозиториев SQLite: тело сущности в колонке Json и колонки, по которым идёт выборка.
/// </summary>
public abstract class BaseSqliteRepository<TEntity, TRow> : ISqliteImportTarget
    where TEntity : class, IHaveStringId
    where TRow : class, ISqliteDocumentRow
{
    protected const string DatabaseUnavailable = "БД недоступна сейчас";

    private const int ExistenceQueryBatchSize = 500;

    protected static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Web;

    protected readonly ILogger _logger;
    protected readonly SqliteDbContext _context;
    protected readonly IApplicationState _appState;
    protected readonly DbSet<TRow> _rows;
    protected readonly CouchDbConnection _configuration;

    protected BaseSqliteRepository(ILogger logger,
        SqliteDbContext context,
        DbSet<TRow> rows,
        IParametersService appConfiguration,
        IApplicationState applicationState)
    {
        _logger = logger;
        _context = context;
        _rows = rows;
        _appState = applicationState;
        _configuration = appConfiguration.Current().Database;
    }

    /// <summary>
    /// Имя базы CouchDB, документы которой попадают в эту таблицу.
    /// </summary>
    public abstract string DatabaseName { get; }

    public virtual async Task<TEntity?> GetById(string id)
    {
        return await ExecuteSafetyDbOperation(
            async () =>
            {
                var row = await _rows.FirstOrDefaultAsync(item => item.Id == id);
                return row == null ? null : ToEntity(row);
            },
            "GetById",
            (TEntity?)null);
    }

    public virtual async Task<bool> Save(TEntity entity)
    {
        if (string.IsNullOrEmpty(entity.Id))
            entity.Id = Guid.NewGuid().ToString();

        return await ExecuteSafetyDbOperation(
            async () =>
            {
                var row = CreateRow(entity, entity.Id);
                var existing = await _rows.FirstOrDefaultAsync(item => item.Id == row.Id);

                if (existing == null)
                    await _rows.AddAsync(row);
                else
                    _context.Entry(existing).CurrentValues.SetValues(row);

                await _context.SaveChangesAsync();

                return true;
            },
            "Save",
            false);
    }

    public virtual async Task<bool> Delete(string id)
    {
        return await ExecuteSafetyDbOperation(
            async () =>
            {
                var row = await _rows.FirstOrDefaultAsync(item => item.Id == id);
                if (row == null)
                    return true;

                _rows.Remove(row);
                await _context.SaveChangesAsync();

                return true;
            },
            "Delete",
            false);
    }

    public async Task<List<TEntity>> GetListById(List<string> ids)
    {
        if (ids.Count == 0)
            return [];

        return await ExecuteSafetyDbOperation(
            async () =>
            {
                var entities = new List<TEntity>();

                foreach (var batch in ids.Distinct(StringComparer.Ordinal).Chunk(ExistenceQueryBatchSize))
                {
                    var batchIds = batch.ToList();
                    var rows = await _rows.Where(row => batchIds.Contains(row.Id)).ToListAsync();

                    foreach (var row in rows)
                    {
                        var entity = ToEntity(row);
                        if (entity != null)
                            entities.Add(entity);
                    }
                }

                return entities;
            },
            "GetListById",
            new List<TEntity>());
    }

    public async Task<bool> SaveBulk(IEnumerable<TEntity> entities)
    {
        var entityList = entities
            .Where(entity => !string.IsNullOrEmpty(entity.Id))
            .GroupBy(entity => entity.Id)
            .Select(group => group.Last())
            .ToList();

        if (entityList.Count == 0)
            return true;

        return await ExecuteSafetyDbOperation(
            async () =>
            {
                var rows = entityList.Select(entity => CreateRow(entity, entity.Id)).ToList();
                var existing = new Dictionary<string, TRow>(StringComparer.Ordinal);

                foreach (var batch in rows.Chunk(ExistenceQueryBatchSize))
                {
                    var batchIds = batch.Select(row => row.Id).ToList();
                    var found = await _rows.Where(row => batchIds.Contains(row.Id)).ToListAsync();

                    foreach (var row in found)
                        existing[row.Id] = row;
                }

                var added = new List<TRow>();

                foreach (var row in rows)
                {
                    if (existing.TryGetValue(row.Id, out var current))
                        _context.Entry(current).CurrentValues.SetValues(row);
                    else
                        added.Add(row);
                }

                if (added.Count > 0)
                    await _rows.AddRangeAsync(added);

                await _context.SaveChangesAsync();

                return true;
            },
            "SaveBulk",
            false);
    }

    public async Task<int> InsertMissing(IReadOnlyList<CouchDbImportDocument> documents, CancellationToken cancellationToken)
    {
        if (documents.Count == 0)
            return 0;

        var existingIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var batch in documents.Chunk(ExistenceQueryBatchSize))
        {
            var batchIds = batch.Select(document => document.Id).ToList();
            var found = await _rows
                .Where(row => batchIds.Contains(row.Id))
                .Select(row => row.Id)
                .ToListAsync(cancellationToken);

            foreach (var id in found)
                existingIds.Add(id);
        }

        var added = new List<TRow>();

        foreach (var document in documents)
        {
            if (existingIds.Contains(document.Id))
                continue;

            if (document.Data.ValueKind == JsonValueKind.Undefined)
                continue;

            var entity = document.Data.Deserialize<TEntity>(JsonOptions);
            if (entity == null)
                continue;

            if (string.IsNullOrEmpty(entity.Id))
                entity.Id = document.Id;

            added.Add(CreateRow(entity, document.Id));
        }

        if (added.Count == 0)
            return 0;

        await _rows.AddRangeAsync(added, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var row in added)
            _context.Entry(row).State = EntityState.Detached;

        return added.Count;
    }

    /// <summary>
    /// Собирает строку таблицы: тело сущности в Json и колонки для выборки.
    /// </summary>
    protected abstract TRow CreateRow(TEntity entity, string id);

    protected TEntity? ToEntity(TRow row) => JsonSerializer.Deserialize<TEntity>(row.Json, JsonOptions);

    protected bool IsOnline() => _configuration.Enable && _appState.CouchDbOnline();

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
                operationName, typeof(TEntity).Name);

            HandleConnectionError(ex);

            return defaultValue;
        }
    }

    private void HandleConnectionError(Exception ex)
    {
        if (!IsConnectionError(ex))
            return;

        _appState.UpdateCouchDbState(false);
    }

    private static bool IsConnectionError(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current is SqliteException || current is IOException)
                return true;
        }

        return false;
    }
}
