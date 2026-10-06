using CouchDb.DatabaseScheme;
using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Documents;
using FmuApiDomain.Documents.Entities;
using FmuApiDomain.Documents.Interfaces;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Logging;
using Sqlite.Documents;
using System.Text.Json;

namespace Sqlite.Repositories;

/// <summary>
/// Документы Frontol в файле SQLite.
/// </summary>
public class DocumentRepository(
    ILogger<DocumentRepository> logger,
    SqliteDbContext context,
    IParametersService appConfiguration,
    IApplicationState applicationState)
    : BaseSqliteRepository<DocumentEntity, DocumentRow>(logger, context, context.Documents, appConfiguration, applicationState), IDocumentRepository
{
    public override string DatabaseName => DatabaseNames.DocumentsDbName;

    public async Task<Result<DocumentEntity>> Add(RequestDocument document)
    {
        DocumentEntity entity = new()
        {
            Id = document.Uid,
            FrontolDocument = document
        };

        var created = await Save(entity);
        if (!created)
            return Result.Failure<DocumentEntity>("Не удалось сохранить документ в CouchDB");

        return Result.Success(entity);
    }

    public async Task<Result<bool>> Delete(RequestDocument document)
    {
        await base.Delete(document.Uid);

        return Result.Success(true);
    }

    public new async Task<Result<bool>> Delete(string uid)
    {
        await base.Delete(uid);

        return Result.Success(true);
    }

    public async Task<Result<DocumentEntity>> Get(string id)
    {
        var data = await GetById(id);

        if (data == null)
            return Result.Failure<DocumentEntity>($"Не найден документ с uid {id}");

        return Result.Success(data);
    }

    protected override DocumentRow CreateRow(DocumentEntity entity, string id) => new()
    {
        Id = id,
        Json = JsonSerializer.Serialize(entity, JsonOptions)
    };
}
