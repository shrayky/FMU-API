using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options.Organization;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Models;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.GisMt;

/// <summary>
/// Загрузка остатка марок из ГИС МТ.
/// </summary>
[AutoRegisterService(ServiceLifetime.Scoped)]
public class GisMtStockLoadService(
    ILogger<GisMtStockLoadService> logger,
    IParametersService parametersService,
    IApplicationState applicationState,
    IGisMtCisesClient cisesClient,
    IGisMtProductGroupsService productGroupsService,
    IGisMtCisInfoSaver cisInfoSaver) : IGisMtStockLoadService
{
    private const int SearchPageSize = 1000;
    private const string IntroducedStatus = "INTRODUCED";
    private const string StockSourceId = "stock";

    private readonly ILogger<GisMtStockLoadService> _logger = logger;
    private readonly IParametersService _parametersService = parametersService;
    private readonly IApplicationState _applicationState = applicationState;
    private readonly IGisMtCisesClient _cisesClient = cisesClient;
    private readonly IGisMtProductGroupsService _productGroupsService = productGroupsService;
    private readonly IGisMtCisInfoSaver _cisInfoSaver = cisInfoSaver;

    /// <summary>
    /// Загружает марки со статусом INTRODUCED для организации.
    /// </summary>
    public async Task<Result<GisMtStockLoadResult>> Load(string inn, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = await _parametersService.CurrentAsync();
            var organisation = GisMtOrganisationResolver.Find(parameters, inn);

            if (organisation is null)
                return Result.Failure<GisMtStockLoadResult>($"Организация с ИНН {inn} не найдена");

            var token = _applicationState.TrueApiToken(organisation.INN).Token;
            if (string.IsNullOrWhiteSpace(token))
                return Result.Failure<GisMtStockLoadResult>($"Нет Bearer-токена для ИНН {organisation.INN}");

            return await LoadForOrganisation(organisation, token, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка загрузки остатка марок для {Inn}", inn);
            return Result.Failure<GisMtStockLoadResult>(ex.Message);
        }
    }

    /// <summary>
    /// Загружает остатки для всех организаций с включённой интеграцией ГИС МТ.
    /// </summary>
    public async Task<Result<GisMtStockLoadAllResult>> LoadAll(CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var marksSaved = 0;
        var organisationsProcessed = 0;

        try
        {
            var parameters = await _parametersService.CurrentAsync();

            foreach (var organisation in parameters.OrganisationConfig.PrintGroups)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (!organisation.TrueApiIntegrationSettings.Enable || string.IsNullOrWhiteSpace(organisation.INN))
                    continue;

                var token = _applicationState.TrueApiToken(organisation.INN).Token;
                if (string.IsNullOrWhiteSpace(token))
                {
                    errors.Add($"Нет Bearer-токена для ИНН {organisation.INN}");
                    continue;
                }

                organisationsProcessed++;

                var loadResult = await LoadForOrganisation(organisation, token, cancellationToken);
                if (loadResult.IsFailure)
                {
                    errors.Add($"{organisation.INN}: {loadResult.Error}");
                    continue;
                }

                marksSaved += loadResult.Value.MarksSaved;
                errors.AddRange(loadResult.Value.Errors);
            }

            return Result.Success(new GisMtStockLoadAllResult(organisationsProcessed, marksSaved, errors));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка массовой загрузки остатков марок ГИС МТ");
            return Result.Failure<GisMtStockLoadAllResult>(ex.Message);
        }
    }

    private async Task<Result<GisMtStockLoadResult>> LoadForOrganisation(
        PrintGroupData organisation,
        string token,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var marksSaved = 0;

        var groupsResult = await _productGroupsService.GetOrRefresh(organisation.INN, cancellationToken);
        if (groupsResult.IsFailure)
            return Result.Failure<GisMtStockLoadResult>(groupsResult.Error);

        foreach (var productGroup in groupsResult.Value)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var loadGroup = await LoadForProductGroup(organisation, token, productGroup, cancellationToken);
            marksSaved += loadGroup.MarksSaved;
            errors.AddRange(loadGroup.Errors);
        }

        return Result.Success(new GisMtStockLoadResult(marksSaved, errors));
    }

    private async Task<(int MarksSaved, List<string> Errors)> LoadForProductGroup(
        PrintGroupData organisation,
        string token,
        string productGroup,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var marksSaved = 0;
        string? lastEmissionDate = null;
        string? lastSgtin = null;
        var isLastPage = false;

        while (!isLastPage && !cancellationToken.IsCancellationRequested)
        {
            var request = new CisSearchRequest
            {
                Filter = new CisSearchFilter
                {
                    States = [new CisSearchState { Status = IntroducedStatus }],
                    ProductGroups = [productGroup]
                },
                Pagination = new CisSearchPagination
                {
                    PerPage = SearchPageSize,
                    LastEmissionDate = lastEmissionDate,
                    Sgtin = lastSgtin,
                    Direction = 0
                }
            };

            var search = await _cisesClient.SearchCises(token, request, cancellationToken);
            if (search.IsFailure)
            {
                errors.Add($"cises/search pg={productGroup}: {search.Error}");
                break;
            }

            var page = search.Value;
            isLastPage = page.IsLastPage || page.Result.Count == 0;

            var cises = page.Result
                .Select(x => x.Cis ?? x.Sgtin)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .Distinct()
                .ToList();

            var saved = await _cisInfoSaver.SaveBatches(
                organisation,
                token,
                productGroup,
                cises,
                StockSourceId,
                cancellationToken);

            if (saved.IsFailure)
                errors.Add(saved.Error);
            else
                marksSaved += saved.Value;

            if (page.Result.Count > 0)
            {
                var last = page.Result[^1];
                lastEmissionDate = last.EmissionDate?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fff'Z'");
                lastSgtin = last.Sgtin ?? last.Cis;
            }
        }

        return (marksSaved, errors);
    }
}
