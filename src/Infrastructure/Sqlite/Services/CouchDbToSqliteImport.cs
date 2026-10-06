using CouchDb.DatabaseScheme;
using CouchDB.Driver;
using CouchDB.Driver.Options;
using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Database.Interfaces;
using FmuApiDomain.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sqlite.Documents;
using System.Text.Json;

namespace Sqlite.Services;

/// <summary>
/// Копирует документы семи баз CouchDB в файл SQLite, не изменяя источник.
/// </summary>
public class CouchDbToSqliteImport(
    ILogger<CouchDbToSqliteImport> logger,
    IParametersService parametersService,
    IServiceScopeFactory scopeFactory) : ICouchDbToSqliteImport
{
    private const int DefaultBatchSize = 1000;
    private const string DesignDocumentPrefix = "_design";

    private static readonly string[] Databases = DatabaseNames.Names();

    private readonly object _gate = new();

    private CouchDbImportState _state = new();

    public Result Start()
    {
        var database = parametersService.Current().Database;

        if (database.Provider != DatabaseProvider.Sqlite)
            return Result.Failure("Перенос доступен при провайдере SQLite: выберите SQLite и перезапустите службу.");

        if (!database.Enable || !database.CouchDbConnectionIsFilled)
            return Result.Failure("Не заполнено подключение CouchDB: адрес, пользователь и пароль.");

        if (!SqliteContextIsRegistered())
            return Result.Failure("Перенос доступен после перезапуска службы с провайдером SQLite.");

        lock (_gate)
        {
            if (_state.Phase == CouchDbImportPhase.Running)
                return Result.Failure(ICouchDbToSqliteImport.AlreadyRunningMessage);

            _state = new CouchDbImportState
            {
                Phase = CouchDbImportPhase.Running,
                CurrentDatabase = Databases[0],
                TotalDatabases = Databases.Length
            };
        }

        _ = Task.Run(() => Run(database));

        return Result.Success();
    }

    private bool SqliteContextIsRegistered()
    {
        using var scope = scopeFactory.CreateScope();

        return scope.ServiceProvider.GetService<SqliteDbContext>() != null;
    }

    public CouchDbImportState State()
    {
        lock (_gate)
            return _state;
    }

    private async Task Run(CouchDbConnection database)
    {
        try
        {
            await using var client = CreateClient(database);

            using var scope = scopeFactory.CreateScope();
            var sqliteContext = scope.ServiceProvider.GetRequiredService<SqliteDbContext>();
            var targets = scope.ServiceProvider.GetServices<ISqliteImportTarget>()
                .ToDictionary(target => target.DatabaseName, StringComparer.Ordinal);

            var completedDatabases = await CountCompletedDatabases(sqliteContext);

            UpdateState(state => Copy(state, completedDatabases: completedDatabases));

            foreach (var databaseName in Databases)
            {
                UpdateState(state => Copy(state, currentDatabase: databaseName, copiedInCurrent: 0));

                if (!targets.TryGetValue(databaseName, out var target))
                {
                    logger.LogWarning("Для базы {Database} нет таблицы SQLite, перенос пропущен", databaseName);
                    continue;
                }

                var wasCompleted = await IsCompleted(sqliteContext, databaseName);
                var copied = await ImportDatabase(client, sqliteContext, target, database, CancellationToken.None);

                if (!wasCompleted)
                    UpdateState(state => Copy(state, completedDatabases: state.CompletedDatabases + 1));

                logger.LogInformation("База {Database} перенесена, записано документов {Count}", databaseName, copied);
            }

            UpdateState(state => Copy(state, phase: CouchDbImportPhase.Completed, error: string.Empty));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Перенос данных из CouchDB в SQLite прерван ошибкой");

            UpdateState(state => Copy(state, phase: CouchDbImportPhase.Failed, error: ex.Message));
        }
    }

    private async Task<int> ImportDatabase(
        CouchClient client,
        SqliteDbContext sqliteContext,
        ISqliteImportTarget target,
        CouchDbConnection database,
        CancellationToken cancellationToken)
    {
        var batchSize = database.BulkBatchSize > 0 ? database.BulkBatchSize : DefaultBatchSize;
        var couchDatabase = client.GetDatabase<JsonDocument>(target.DatabaseName);

        var progress = await sqliteContext.ImportProgress
            .FirstOrDefaultAsync(row => row.Id == target.DatabaseName, cancellationToken);

        if (progress == null)
        {
            progress = new ImportProgressRow { Id = target.DatabaseName };
            await sqliteContext.ImportProgress.AddAsync(progress, cancellationToken);
        }

        // завершённую базу обходим с начала и дописываем только отсутствующие Id
        var lastId = progress.Completed ? string.Empty : progress.LastId;
        var copied = 0;

        while (true)
        {
            var request = couchDatabase.NewRequest()
                .AppendPathSegment("_all_docs")
                .SetQueryParam("include_docs", "true")
                .SetQueryParam("limit", batchSize);

            if (!string.IsNullOrEmpty(lastId))
            {
                request = request
                    .SetQueryParam("startkey", JsonSerializer.Serialize(lastId))
                    .SetQueryParam("skip", 1);
            }

            var answer = await request.GetStringAsync(cancellationToken);
            var (documents, batchLastId, rows) = ParseBatch(answer);

            if (rows == 0)
                break;

            if (!string.IsNullOrEmpty(batchLastId))
                lastId = batchLastId;

            var inserted = await target.InsertMissing(documents, cancellationToken);
            copied += inserted;

            progress.LastId = lastId;
            await sqliteContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("База {Database}: прочитано документов {Rows}, записано новых {Inserted}",
                target.DatabaseName, rows, inserted);

            UpdateState(state => Copy(state, copiedInCurrent: copied));
        }

        progress.Completed = true;
        progress.LastId = string.Empty;
        await sqliteContext.SaveChangesAsync(cancellationToken);

        return copied;
    }

    private async Task<int> CountCompletedDatabases(SqliteDbContext sqliteContext)
    {
        return await sqliteContext.ImportProgress
            .Where(row => row.Completed && Databases.Contains(row.Id))
            .CountAsync();
    }

    private static async Task<bool> IsCompleted(SqliteDbContext sqliteContext, string databaseName)
    {
        var progress = await sqliteContext.ImportProgress
            .FirstOrDefaultAsync(row => row.Id == databaseName);

        return progress?.Completed == true;
    }

    private static (List<CouchDbImportDocument> Documents, string LastId, int Rows) ParseBatch(string answer)
    {
        var documents = new List<CouchDbImportDocument>();
        var lastId = string.Empty;
        var rows = 0;

        using var document = JsonDocument.Parse(answer);

        if (!document.RootElement.TryGetProperty("rows", out var rowsElement) || rowsElement.ValueKind != JsonValueKind.Array)
            return (documents, lastId, rows);

        foreach (var row in rowsElement.EnumerateArray())
        {
            rows++;

            if (!row.TryGetProperty("id", out var idElement))
                continue;

            var id = idElement.GetString();
            if (string.IsNullOrEmpty(id))
                continue;

            lastId = id;

            if (id.StartsWith(DesignDocumentPrefix, StringComparison.Ordinal))
                continue;

            if (!row.TryGetProperty("doc", out var docElement) || docElement.ValueKind != JsonValueKind.Object)
                continue;

            var data = docElement.TryGetProperty("data", out var dataElement)
                ? dataElement.Clone()
                : default;

            documents.Add(new CouchDbImportDocument(id, data));
        }

        return (documents, lastId, rows);
    }

    private static CouchClient CreateClient(CouchDbConnection database)
    {
        var httpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        })
        {
            Timeout = TimeSpan.FromSeconds(database.QueryTimeoutSeconds)
        };

        var clientOptions = new CouchClientOptions
        {
            HttpClient = httpClient,
            ThrowOnQueryWarning = false,
            JsonSerializerOptions = JsonSerializerOptions.Web
        };

        return new CouchClient(
            database.NetAddress,
            new BasicCredentials(database.UserName, database.Password),
            clientOptions);
    }

    private void UpdateState(Func<CouchDbImportState, CouchDbImportState> update)
    {
        lock (_gate)
            _state = update(_state);
    }

    private static CouchDbImportState Copy(
        CouchDbImportState state,
        CouchDbImportPhase? phase = null,
        string? currentDatabase = null,
        int? copiedInCurrent = null,
        int? completedDatabases = null,
        string? error = null) => new()
        {
            Phase = phase ?? state.Phase,
            CurrentDatabase = currentDatabase ?? state.CurrentDatabase,
            CopiedInCurrent = copiedInCurrent ?? state.CopiedInCurrent,
            CompletedDatabases = completedDatabases ?? state.CompletedDatabases,
            TotalDatabases = state.TotalDatabases,
            Error = error ?? state.Error
        };
}
