using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Database.Dto;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.MarkInformation.Models;
using FmuApiDomain.Repositories;
using FmuApiDomain.State.Interfaces;

namespace FmuApiApplication.Mark.Services;

public class MarksListService(
    IMarkInformationRepository markRepository,
    ICheckStatisticRepository checkStatisticRepository,
    IParametersService parametersService,
    IApplicationState appState) : IMarksListService
{
    public const string DatabaseDisabled = "База данных отключена";
    public const string DatabaseUnavailable = "База данных недоступна в данный момент";
    public const string CheckNotFound = "Нет сохранённой проверки для этой марки";

    public async Task<Result<MarkSearchResult>> List(string searchTerm, int page, int pageSize)
    {
        var unavailable = await DatabaseError();
        if (unavailable != null)
            return Result.Failure<MarkSearchResult>(unavailable);

        if (page < 1)
            page = 1;

        if (pageSize < 1 || pageSize > 100)
            pageSize = 50;

        var result = await markRepository.SearchMarkData(searchTerm ?? string.Empty, page, pageSize);
        if (result.IsFailure)
            return result;

        await FillCheckIds(result.Value.Marks);
        return result;
    }

    public async Task<Result<StatisticEntity>> CheckInformation(string id)
    {
        var unavailable = await DatabaseError();
        if (unavailable != null)
            return Result.Failure<StatisticEntity>(unavailable);

        var check = await checkStatisticRepository.ById(id);
        if (check == null)
            return Result.Failure<StatisticEntity>(CheckNotFound);

        return Result.Success(check);
    }

    private async Task FillCheckIds(List<MarkListItem> marks)
    {
        var sgtins = marks
            .Select(mark => mark.MarkId)
            .Where(sgtin => !string.IsNullOrEmpty(sgtin))
            .Distinct()
            .ToList();

        if (sgtins.Count == 0)
            return;

        var lastCheckIds = await checkStatisticRepository.LastCheckIds(sgtins);
        foreach (var mark in marks)
        {
            if (lastCheckIds.TryGetValue(mark.MarkId, out var checkId))
                mark.CheckId = checkId;
        }
    }

    private async Task<string?> DatabaseError()
    {
        var settings = await parametersService.CurrentAsync();

        if (!settings.Database.Enable)
            return DatabaseDisabled;

        if (!appState.CouchDbOnline())
            return DatabaseUnavailable;

        return null;
    }
}
