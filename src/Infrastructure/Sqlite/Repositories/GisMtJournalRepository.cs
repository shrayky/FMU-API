using CSharpFunctionalExtensions;
using CouchDb.DatabaseScheme;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Journal;
using FmuApiDomain.GisMt.Models;
using FmuApiDomain.State.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sqlite.Documents;
using System.Text.Json;

namespace Sqlite.Repositories;

/// <summary>
/// Журнал исходящих документов ГИС МТ в файле SQLite.
/// </summary>
public class GisMtJournalRepository(
    ILogger<GisMtJournalRepository> logger,
    SqliteDbContext context,
    IParametersService appConfiguration,
    IApplicationState applicationState)
    : BaseSqliteRepository<GisMtJournalEntity, GisMtJournalRow>(logger, context, context.GisMtJournal, appConfiguration, applicationState), IGisMtJournalRepository
{
    public override string DatabaseName => DatabaseNames.GisMtJournalDbName;

    public async Task<GisMtJournalEntity?> Get(string id)
    {
        // AsNoTracking: после условной записи статуса через UPDATE чтение не должно отдавать
        // закэшированную сущность с прежним статусом.
        return await ExecuteSafetyDbOperation(
            async () =>
            {
                var row = await _rows.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
                return row == null ? null : ToEntity(row);
            },
            "Get",
            (GisMtJournalEntity?)null);
    }

    public async Task<GisMtJournalEntity?> FindByUid(string uid)
    {
        if (string.IsNullOrWhiteSpace(uid))
            return null;

        return await ExecuteSafetyDbOperation(
            async () =>
            {
                var row = await _rows.AsNoTracking().FirstOrDefaultAsync(item => item.Uid == uid);
                return row == null ? null : ToEntity(row);
            },
            "FindByUid",
            (GisMtJournalEntity?)null);
    }

    public async Task<Result<GisMtJournalPage>> List(string kind, string status, int page, int pageSize)
    {
        return await ExecuteSafetyDbOperation(
            async () =>
            {
                var query = _rows.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(kind))
                    query = query.Where(row => row.Kind == kind);

                if (!string.IsNullOrWhiteSpace(status))
                    query = query.Where(row => row.Status == status);

                var count = await query.CountAsync();
                var rows = await query
                    .OrderByDescending(row => row.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var documents = rows
                    .Select(ToEntity)
                    .Where(document => document != null)
                    .Cast<GisMtJournalEntity>()
                    .ToList();

                return Result.Success(new GisMtJournalPage
                {
                    Documents = documents,
                    Count = count,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalPages = count == 0 ? 1 : (int)Math.Ceiling(count / (double)pageSize)
                });
            },
            "List",
            Result.Failure<GisMtJournalPage>(DatabaseUnavailable));
    }

    /// <summary>
    /// Отправленные акты, у которых статус в ГИС МТ ещё не окончательный.
    /// </summary>
    public async Task<List<GisMtJournalEntity>> GetPendingCrptStatus(DateTime checkedBefore, DateTime sentAfter, int limit)
    {
        var rows = await ExecuteSafetyDbOperation(
            async () => await _rows
                .AsNoTracking()
                .Where(row => row.Status == GisMtJournalStatuses.Sent)
                .OrderByDescending(row => row.CreatedAt)
                .Take(limit * 4)
                .ToListAsync(),
            "GetPendingCrptStatus",
            (List<GisMtJournalRow>?)null);

        if (rows == null)
            return [];

        return rows
            .Select(ToEntity)
            .Where(document => document != null && GisMtCrptStatuses.NeedsPolling(document, checkedBefore, sentAfter))
            .Select(document => document!)
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

        var failed = GisMtCrptStatuses.Failed;

        var rows = await ExecuteSafetyDbOperation(
            async () => await _rows
                .AsNoTracking()
                .Where(row => row.Status == GisMtJournalStatuses.Sent
                    && row.Inn == inn
                    && row.ProductGroup == group
                    && !failed.Contains(row.CrptStatus)
                    && (row.SentAt ?? row.CreatedAt) >= sentAfter)
                .OrderByDescending(row => row.SentAt ?? row.CreatedAt)
                .Take(limit)
                .ToListAsync(),
            "GetSubmittedCis",
            (List<GisMtJournalRow>?)null);

        if (rows == null)
            return Result.Failure<HashSet<string>>("Не удалось прочитать отправленные акты ГИС МТ");

        var cises = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var document in rows.Select(ToEntity).Where(document => document != null))
        {
            foreach (var line in document!.Lines)
                cises.Add((line.Cis ?? string.Empty).Trim());
        }

        return Result.Success(cises);
    }

    public async Task<bool> TryUpdateStatus(GisMtJournalEntity entity, string expectedStatus)
    {
        var row = CreateRow(entity, entity.Id);

        return await ExecuteSafetyDbOperation(
            async () => await _rows
                .Where(item => item.Id == row.Id && item.Status == expectedStatus)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.Json, row.Json)
                    .SetProperty(item => item.Status, row.Status)
                    .SetProperty(item => item.Uid, row.Uid)
                    .SetProperty(item => item.Inn, row.Inn)
                    .SetProperty(item => item.ProductGroup, row.ProductGroup)
                    .SetProperty(item => item.SentAt, row.SentAt)
                    .SetProperty(item => item.CrptStatus, row.CrptStatus)) > 0,
            "TryUpdateStatus",
            false);
    }

    protected override GisMtJournalRow CreateRow(GisMtJournalEntity entity, string id)
    {
        var row = new GisMtJournalRow();

        GisMtJournalRowMapper.Fill(row, entity, id);
        row.Json = JsonSerializer.Serialize(entity, JsonOptions);

        return row;
    }
}
