using CSharpFunctionalExtensions;
using CouchDb.Documents;
using CouchDb.Queries;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Journal;
using FmuApiDomain.GisMt.Models;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Logging;

namespace CouchDb.Repositories;

/// <summary>
/// Журнал исходящих документов ГИС МТ в CouchDB.
/// </summary>
public class GisMtJournalRepository : BaseCouchDbRepository<GisMtJournalEntity>, IGisMtJournalRepository
{
    public GisMtJournalRepository(
        ILogger<GisMtJournalRepository> logger,
        CouchDbContext context,
        IParametersService appConfiguration,
        IApplicationState applicationState)
        : base(logger, context, context.GisMtJournal, appConfiguration, applicationState)
    {
    }

    public async Task<GisMtJournalEntity?> Get(string id)
    {
        if (_context == null)
            return null;

        return await GetByIdAsync(id);
    }

    public async Task<GisMtJournalEntity?> FindByUid(string uid)
    {
        if (_context == null || string.IsNullOrWhiteSpace(uid))
            return null;

        var result = await ExecuteMangoQueryAsync(GisMtJournalMangoQueryBuilder.BuildUidQuery(uid));

        return result.IsFailure ? null : result.Value.FirstOrDefault();
    }

    public async Task<bool> Save(GisMtJournalEntity entity)
    {
        if (_context == null)
            return false;

        if (string.IsNullOrEmpty(entity.Id))
            return await CreateAsync(entity);

        var existing = await GetByIdAsync(entity.Id);

        if (existing == null)
            return await CreateAsync(entity);

        return await UpdateAsync(entity.Id, entity);
    }

    public async Task<bool> TryUpdateStatus(GisMtJournalEntity entity, string expectedStatus)
    {
        if (_context == null)
            return false;

        return await ExecuteSafetyDbOperation(
            async () =>
            {
                var response = await _database.ReadItemAsync(entity.Id);

                if (response == null)
                    return false;

                var current = response.Document.ToDomain();

                if (!string.Equals(current.Status, expectedStatus, StringComparison.Ordinal))
                    return false;

                var document = CouchDoc<GisMtJournalEntity>.FromDomain(entity, entity.Id);
                await _database.UpdateItemAsync(document, entity.Id, response.Rev);

                return true;
            },
            "TryUpdateStatus",
            false);
    }

    public async Task<bool> Delete(string id)
    {
        if (_context == null)
            return false;

        return await DeleteAsync(id);
    }

    public async Task<List<GisMtJournalEntity>> GetPendingCrptStatus(DateTime checkedBefore, DateTime sentAfter, int limit)
    {
        if (_context == null)
            return [];

        var documents = await ExecuteMangoQueryAsync(
            GisMtJournalMangoQueryBuilder.BuildStatusQuery(GisMtJournalStatuses.Sent, limit * 4));

        if (documents.IsFailure)
            return [];

        return documents.Value
            .Where(document => GisMtCrptStatuses.NeedsPolling(document, checkedBefore, sentAfter))
            .Take(limit)
            .ToList();
    }

    /// <summary>
    /// КИ, которые уже уходили в ГИС МТ в отправленных актах этой организации и товарной группы.
    /// </summary>
    public async Task<Result<HashSet<string>>> GetSubmittedCis(string organisationInn, string productGroup, DateTime sentAfter, int limit)
    {
        var inn = (organisationInn ?? string.Empty).Trim();
        var group = (productGroup ?? string.Empty).Trim();

        if (inn.Length == 0 || group.Length == 0)
            return Result.Success(new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        if (_context == null)
            return Result.Failure<HashSet<string>>(DatabaseUnavailable);

        var withSentAt = await ExecuteMangoQueryAsync(
            GisMtJournalMangoQueryBuilder.BuildSubmittedQuery(inn, group, sentAfter, limit));

        if (withSentAt.IsFailure)
            return Result.Failure<HashSet<string>>(withSentAt.Error);

        // Старые акты без даты отправки: по ним срок считается от даты создания.
        var withoutSentAt = await ExecuteMangoQueryAsync(
            GisMtJournalMangoQueryBuilder.BuildSubmittedWithoutSentAtQuery(inn, group, sentAfter, limit));

        if (withoutSentAt.IsFailure)
            return Result.Failure<HashSet<string>>(withoutSentAt.Error);

        var cises = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddCises(cises, withSentAt.Value);
        AddCises(cises, withoutSentAt.Value);

        return Result.Success(cises);
    }

    private static void AddCises(HashSet<string> cises, IEnumerable<GisMtJournalEntity> documents)
    {
        foreach (var document in documents)
        {
            foreach (var line in document.Lines)
                cises.Add((line.Cis ?? string.Empty).Trim());
        }
    }

    public async Task<Result<GisMtJournalPage>> List(string kind, string status, int page, int pageSize)
    {        if (_context == null)
            return Result.Failure<GisMtJournalPage>(DatabaseUnavailable);

        var documents = await ExecuteMangoQueryAsync(
            GisMtJournalMangoQueryBuilder.BuildPageQuery(kind, status, page, pageSize));

        if (documents.IsFailure)
            return Result.Failure<GisMtJournalPage>(documents.Error);

        int totalCount;

        if (string.IsNullOrWhiteSpace(kind) && string.IsNullOrWhiteSpace(status))
        {
            var documentsCount = await GetDocumentsCountAsync();

            if (documentsCount == null)
                return Result.Failure<GisMtJournalPage>("Не удалось получить число документов");

            totalCount = documentsCount.Value;
        }
        else
        {
            var appConfig = await _appConfiguration.CurrentAsync();
            var count = await ExecuteMangoCountAsync(
                GisMtJournalMangoQueryBuilder.BuildCountQuery(kind, status, appConfig.Database.QueryLimit));

            if (count.IsFailure)
                return Result.Failure<GisMtJournalPage>(count.Error);

            totalCount = count.Value;
        }

        return Result.Success(new GisMtJournalPage
        {
            Documents = documents.Value,
            Count = totalCount,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = totalCount == 0 ? 1 : (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }
}
