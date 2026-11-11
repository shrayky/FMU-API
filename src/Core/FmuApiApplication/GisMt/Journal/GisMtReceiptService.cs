using CSharpFunctionalExtensions;
using FmuApiApplication.Mark.Interfaces;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Frontol.Interfaces;
using FmuApiDomain.GisMt;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Journal;
using FmuApiDomain.GisMt.Models;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.GisMt.Journal;

/// <summary>
/// Акт списания весовой молочной продукции: КИ, GTIN, название, количество, цена за килограмм и сумма строки.
/// </summary>
[AutoRegisterService(ServiceLifetime.Scoped)]
public class GisMtReceiptService : IGisMtReceiptService
{
    private const string MilkProductGroup = "milk";
    private const int SubmittedCisDays = 30;
    private const int SubmittedActsLimit = 500;

    private readonly ILogger<GisMtReceiptService> _logger;
    private readonly IParametersService _parametersService;
    private readonly IApplicationState _applicationState;
    private readonly IMarkParser _markParser;
    private readonly IGisMtCisesClient _cisesClient;
    private readonly IFrontolSprTServiceFactory _sprtFactory;
    private readonly IGisMtJournalService _journalService;
    private readonly IGisMtMarkRepository _markRepository;
    private readonly IGisMtJournalRepository _journalRepository;
    private readonly IGisMtCisAggregationClient _aggregationClient;

    public GisMtReceiptService(
        ILogger<GisMtReceiptService> logger,
        IParametersService parametersService,
        IApplicationState applicationState,
        IMarkParser markParser,
        IGisMtCisesClient cisesClient,
        IFrontolSprTServiceFactory sprtFactory,
        IGisMtJournalService journalService,
        IGisMtMarkRepository markRepository,
        IGisMtJournalRepository journalRepository,
        IGisMtCisAggregationClient aggregationClient)
    {
        _logger = logger;
        _parametersService = parametersService;
        _applicationState = applicationState;
        _markParser = markParser;
        _cisesClient = cisesClient;
        _sprtFactory = sprtFactory;
        _journalService = journalService;
        _markRepository = markRepository;
        _journalRepository = journalRepository;
        _aggregationClient = aggregationClient;
    }

    public async Task<Result<List<GisMtReceiptLinePreview>>> Preview(string inn, IReadOnlyList<string> marks, CancellationToken cancellationToken = default)
    {
        var parameters = await _parametersService.CurrentAsync();
        var organisation = GisMtOrganisationResolver.Find(parameters, inn ?? string.Empty);

        if (organisation is null)
            return Result.Failure<List<GisMtReceiptLinePreview>>($"Организация с ИНН {inn} не найдена");

        return Result.Success(await BuildPreview(organisation.INN, parameters, marks, cancellationToken));
    }

    public async Task<Result<GisMtReceiptCreateResult>> Create(string inn, IReadOnlyList<string> marks, CancellationToken cancellationToken = default)
    {
        var parameters = await _parametersService.CurrentAsync();
        var organisation = GisMtOrganisationResolver.Find(parameters, inn ?? string.Empty);

        if (organisation is null)
            return Result.Failure<GisMtReceiptCreateResult>($"Организация с ИНН {inn} не найдена");

        var preview = await BuildPreview(organisation.INN, parameters, marks, cancellationToken);
        var lines = ToLines(preview);

        var lineErrors = preview
            .Where(item => item.Cis.Length == 0)
            .Select(item => item.Error)
            .ToList();

        if (lines.Count == 0)
            return Result.Failure<GisMtReceiptCreateResult>(lineErrors.Count == 0
                ? "В документе нет ни одной марки"
                : string.Join("; ", lineErrors));

        var document = new GisMtJournalEntity
        {
            Id = Guid.NewGuid().ToString(),
            Kind = GisMtJournalKinds.LkReceipt,
            Reason = GisMtWriteOffReasons.Packing,
            ProductGroup = MilkProductGroup,
            Status = GisMtJournalStatuses.Draft,
            Inn = organisation.INN,
            Kpp = organisation.Kpp,
            FiasId = organisation.FiasId,
            DocDate = DateOnly.FromDateTime(DateTime.Now),
            CreatedAt = DateTime.Now,
            Lines = lines
        };

        var created = await _journalService.Create(document);

        if (created.IsFailure)
            return Result.Failure<GisMtReceiptCreateResult>(created.Error);

        return Result.Success(new GisMtReceiptCreateResult(created.Value, lineErrors));
    }

    public async Task<Result<GisMtReceiptCreateResult>> Update(string id, string inn, IReadOnlyList<string> marks, CancellationToken cancellationToken = default)
    {
        var document = await _journalService.Get(id);

        if (document is null)
            return Result.Failure<GisMtReceiptCreateResult>("Акт журнала ГИС МТ не найден");

        if (document.Kind != GisMtJournalKinds.LkReceipt)
            return Result.Failure<GisMtReceiptCreateResult>("Редактировать можно только списание молочной продукции");

        if (document.Status == GisMtJournalStatuses.Sent)
            return Result.Failure<GisMtReceiptCreateResult>("Отправленный документ редактировать нельзя");

        if (GisMtJournalStatuses.IsLocked(document.Status))
            return Result.Failure<GisMtReceiptCreateResult>("Документ отправляется, редактировать его нельзя");

        var parameters = await _parametersService.CurrentAsync();
        var organisation = GisMtOrganisationResolver.Find(parameters, string.IsNullOrWhiteSpace(inn) ? document.Inn : inn);

        if (organisation is null)
            return Result.Failure<GisMtReceiptCreateResult>($"Организация с ИНН {inn} не найдена");

        var preview = await BuildPreview(organisation.INN, parameters, marks, cancellationToken);
        ReuseStoredLines(document, preview);
        var lines = ToLines(preview);

        var lineErrors = preview
            .Where(item => item.Cis.Length == 0)
            .Select(item => item.Error)
            .ToList();

        if (lines.Count == 0)
            return Result.Failure<GisMtReceiptCreateResult>(lineErrors.Count == 0
                ? "В документе нет ни одной марки"
                : string.Join("; ", lineErrors));

        // Пока собирались строки, акт могли отправить: перечитываем, чтобы не затереть sent и идентификатор Честного знака.
        var current = await ReloadDraft(id);

        if (current.IsFailure)
            return Result.Failure<GisMtReceiptCreateResult>(current.Error);

        var target = current.Value;

        target.Inn = organisation.INN;
        target.Kpp = organisation.Kpp;
        target.FiasId = organisation.FiasId;
        target.Lines = lines;
        target.ErrorText = string.Empty;

        if (string.IsNullOrWhiteSpace(target.ProductGroup))
            target.ProductGroup = MilkProductGroup;

        if (target.Status == GisMtJournalStatuses.Error)
            target.Status = GisMtJournalStatuses.Draft;

        if (!await _journalRepository.Save(target))
            return Result.Failure<GisMtReceiptCreateResult>("Не удалось сохранить акт журнала ГИС МТ");

        return Result.Success(new GisMtReceiptCreateResult(target, lineErrors));
    }

    public async Task<Result<GisMtReceiptCreateResult>> CreateExpiration(string inn, string productGroup, IReadOnlyList<GisMtExpiredLine> lines, CancellationToken cancellationToken = default)
    {
        var group = (productGroup ?? string.Empty).Trim();

        if (group.Length == 0)
            return Result.Failure<GisMtReceiptCreateResult>("Выберите товарную группу");

        var parameters = await _parametersService.CurrentAsync();
        var organisation = GisMtOrganisationResolver.Find(parameters, inn ?? string.Empty);

        if (organisation is null)
            return Result.Failure<GisMtReceiptCreateResult>($"Организация с ИНН {inn} не найдена");

        var (documentLines, lineErrors) = BuildExpirationLines(lines);

        if (documentLines.Count == 0)
            return Result.Failure<GisMtReceiptCreateResult>(lineErrors.Count == 0
                ? "В документе нет ни одной марки"
                : string.Join("; ", lineErrors));

        var document = new GisMtJournalEntity
        {
            Id = Guid.NewGuid().ToString(),
            Kind = GisMtJournalKinds.LkReceipt,
            Reason = GisMtWriteOffReasons.Expiration,
            ProductGroup = group,
            Status = GisMtJournalStatuses.Draft,
            Inn = organisation.INN,
            Kpp = organisation.Kpp,
            FiasId = organisation.FiasId,
            DocDate = DateOnly.FromDateTime(DateTime.Now),
            CreatedAt = DateTime.Now,
            Lines = documentLines
        };

        var created = await _journalService.Create(document);

        if (created.IsFailure)
            return Result.Failure<GisMtReceiptCreateResult>(created.Error);

        return Result.Success(new GisMtReceiptCreateResult(created.Value, lineErrors));
    }

    public async Task<Result<GisMtReceiptCreateResult>> UpdateExpiration(string id, string inn, string productGroup, IReadOnlyList<GisMtExpiredLine> lines, CancellationToken cancellationToken = default)
    {
        var document = await _journalService.Get(id);

        if (document is null)
            return Result.Failure<GisMtReceiptCreateResult>("Акт журнала ГИС МТ не найден");

        if (document.Kind != GisMtJournalKinds.LkReceipt)
            return Result.Failure<GisMtReceiptCreateResult>("Редактировать можно только списание молочной продукции");

        if (!GisMtWriteOffReasons.IsExpiration(document.Reason))
            return Result.Failure<GisMtReceiptCreateResult>("Документ создан по другой причине выбытия");

        var group = (productGroup ?? string.Empty).Trim();

        if (group.Length == 0)
            group = (document.ProductGroup ?? string.Empty).Trim();

        if (group.Length == 0)
            return Result.Failure<GisMtReceiptCreateResult>("Выберите товарную группу");

        if (document.Status == GisMtJournalStatuses.Sent)
            return Result.Failure<GisMtReceiptCreateResult>("Отправленный документ редактировать нельзя");

        if (GisMtJournalStatuses.IsLocked(document.Status))
            return Result.Failure<GisMtReceiptCreateResult>("Документ отправляется, редактировать его нельзя");

        var parameters = await _parametersService.CurrentAsync();
        var organisation = GisMtOrganisationResolver.Find(parameters, string.IsNullOrWhiteSpace(inn) ? document.Inn : inn);

        if (organisation is null)
            return Result.Failure<GisMtReceiptCreateResult>($"Организация с ИНН {inn} не найдена");

        var (documentLines, lineErrors) = BuildExpirationLines(lines);

        if (documentLines.Count == 0)
            return Result.Failure<GisMtReceiptCreateResult>(lineErrors.Count == 0
                ? "В документе нет ни одной марки"
                : string.Join("; ", lineErrors));

        // Пока собирались строки, акт могли отправить: перечитываем, чтобы не затереть sent и идентификатор Честного знака.
        var current = await ReloadDraft(id);

        if (current.IsFailure)
            return Result.Failure<GisMtReceiptCreateResult>(current.Error);

        var target = current.Value;

        target.Inn = organisation.INN;
        target.Kpp = organisation.Kpp;
        target.FiasId = organisation.FiasId;
        target.ProductGroup = group;
        target.Lines = documentLines;
        target.ErrorText = string.Empty;

        if (target.Status == GisMtJournalStatuses.Error)
            target.Status = GisMtJournalStatuses.Draft;

        if (!await _journalRepository.Save(target))
            return Result.Failure<GisMtReceiptCreateResult>("Не удалось сохранить акт журнала ГИС МТ");

        return Result.Success(new GisMtReceiptCreateResult(target, lineErrors));
    }

    public async Task<Result<List<string>>> Groups(string inn, CancellationToken cancellationToken = default)
    {
        var parameters = await _parametersService.CurrentAsync();
        var organisation = GisMtOrganisationResolver.Find(parameters, inn ?? string.Empty);

        if (organisation is null)
            return Result.Failure<List<string>>($"Организация с ИНН {inn} не найдена");

        var groups = await _markRepository.GetProductGroups(organisation.INN, GisMtJournalLimits.StockGroups);

        return Result.Success(groups);
    }

    public async Task<Result<int>> ExpiredCount(string inn, string productGroup, CancellationToken cancellationToken = default)
    {
        var group = (productGroup ?? string.Empty).Trim();

        if (group.Length == 0)
            return Result.Failure<int>("Выберите товарную группу");

        var parameters = await _parametersService.CurrentAsync();
        var organisation = GisMtOrganisationResolver.Find(parameters, inn ?? string.Empty);

        if (organisation is null)
            return Result.Failure<int>($"Организация с ИНН {inn} не найдена");

        var search = await _markRepository.Search(
            string.Empty,
            1,
            1,
            productGroup: group,
            expiredOnly: true,
            sold: false,
            organisationInn: organisation.INN);

        if (search.IsFailure)
            return Result.Failure<int>(search.Error);

        return Result.Success(search.Value.Count);
    }

    public async Task<Result<GisMtExpiredSelection>> FillExpired(string inn, string productGroup, CancellationToken cancellationToken = default)
    {
        var group = (productGroup ?? string.Empty).Trim();

        if (group.Length == 0)
            return Result.Failure<GisMtExpiredSelection>("Выберите товарную группу");

        var parameters = await _parametersService.CurrentAsync();
        var organisation = GisMtOrganisationResolver.Find(parameters, inn ?? string.Empty);

        if (organisation is null)
            return Result.Failure<GisMtExpiredSelection>($"Организация с ИНН {inn} не найдена");

        var search = await _markRepository.Search(
            string.Empty,
            1,
            GisMtJournalLimits.MaxExpiredMarksPerFill,
            productGroup: group,
            expiredOnly: true,
            sold: false,
            organisationInn: organisation.INN);

        if (search.IsFailure)
            return Result.Failure<GisMtExpiredSelection>(search.Error);

        var submitted = await _journalRepository.GetSubmittedCis(
            organisation.INN,
            group,
            DateTime.Now.AddDays(-SubmittedCisDays),
            SubmittedActsLimit);

        if (submitted.IsFailure)
            return Result.Failure<GisMtExpiredSelection>(submitted.Error);

        var cises = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var marks = search.Value.Marks
            .Select(mark => new GisMtExpiredMark(
                MarkId(mark),
                GisMtCisNormalizer.ToCis(mark.Cis),
                string.IsNullOrWhiteSpace(mark.Gtin) ? GisMtCisGtin.Extract(mark.Cis) : mark.Gtin,
                mark.ExpireDate))
            .Where(mark => mark.Mark.Length > 0 && mark.Cis.Length > 0 && cises.Add(mark.Cis))
            .Where(mark => !submitted.Value.Contains(mark.Cis))
            .ToList();

        marks = UniqueByCis(marks, mark => mark.Cis);

        return Result.Success(await ExcludePackages(organisation.INN, group, marks, cancellationToken));
    }

    /// <summary>
    /// Перечитывает черновик перед записью: пока собирались строки, акт могли отправить,
    /// и запись всего акта затёрла бы статус sent, дату и идентификатор Честного знака.
    /// </summary>
    private async Task<Result<GisMtJournalEntity>> ReloadDraft(string id)
    {
        var document = await _journalRepository.Get(id);

        if (document is null)
            return Result.Failure<GisMtJournalEntity>("Акт журнала ГИС МТ не найден");

        if (document.Status is not (GisMtJournalStatuses.Draft or GisMtJournalStatuses.Error))
            return Result.Failure<GisMtJournalEntity>("Документ уже отправлен или отправляется, редактировать его нельзя");

        return Result.Success(document);
    }

    /// <summary>
    /// Убирает из подбора упаковку, если её вложения уже в подборе: иначе ГИС МТ считает вложение повтором.
    /// Упаковка без вложений в подборе остаётся — она выбывает целиком.
    /// </summary>
    private async Task<GisMtExpiredSelection> ExcludePackages(
        string inn,
        string productGroup,
        List<GisMtExpiredMark> marks,
        CancellationToken cancellationToken)
    {
        if (marks.Count == 0)
            return new GisMtExpiredSelection(marks, 0);

        var token = _applicationState.TrueApiToken(inn).Token;

        if (string.IsNullOrWhiteSpace(token))
            return new GisMtExpiredSelection(marks, 0);

        var info = await _cisesClient.CisesInfo(token, marks.Select(mark => mark.Cis).ToList(), productGroup, cancellationToken);

        if (info.IsFailure)
        {
            _logger.LogWarning("Типы упаковок марок подбора не получены: {Error}", info.Error);
            return new GisMtExpiredSelection(marks, 0);
        }

        var packages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in info.Value)
        {
            var cis = item.CisInfo?.Cis ?? item.CisInfo?.RequestedCis;
            var packageType = item.CisInfo?.PackageType;

            if (string.IsNullOrWhiteSpace(cis) || string.IsNullOrWhiteSpace(packageType))
                continue;

            if (!string.Equals(packageType.Trim(), "UNIT", StringComparison.OrdinalIgnoreCase))
                packages.Add(GisMtCisNormalizer.ToCis(cis));
        }

        if (packages.Count == 0)
            return new GisMtExpiredSelection(marks, 0);

        var units = marks
            .Where(mark => !packages.Contains(mark.Cis))
            .Select(mark => mark.Cis)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var aggregation = await _aggregationClient.AggregatedList(
            token,
            packages.ToList(),
            productGroup,
            cancellationToken);

        if (aggregation.IsFailure)
        {
            // Без вложений нельзя доказать, что упаковка содержит выбранные марки:
            // при сбое агрегации упаковки не выбрасываем, иначе они молча не выбывают.
            _logger.LogWarning("Вложения упаковок подбора не получены: {Error}", aggregation.Error);

            return new GisMtExpiredSelection(marks, 0);
        }

        var kept = new List<GisMtExpiredMark>();
        var excluded = 0;

        foreach (var mark in marks)
        {
            if (!packages.Contains(mark.Cis))
            {
                kept.Add(mark);
                continue;
            }

            var hasSelectedChild = aggregation.Value.TryGetValue(mark.Cis, out var children)
                && children.Any(child => units.Contains(GisMtCisNormalizer.ToCis(child)));

            if (hasSelectedChild)
                excluded++;
            else
                kept.Add(mark);
        }

        return new GisMtExpiredSelection(kept, excluded);
    }

    private static (List<GisMtJournalLine> Lines, List<string> Errors) BuildExpirationLines(IReadOnlyList<GisMtExpiredLine> source)
    {
        var candidates = new List<GisMtJournalLine>();
        var errors = new List<string>();

        foreach (var item in source ?? [])
        {
            var mark = (item.Mark ?? string.Empty).Trim();
            var cis = GisMtCisNormalizer.ToCis(item.Cis);
            var gtin = GisMtCisGtin.Extract(cis);

            if (cis.Length == 0 || gtin.Length == 0)
            {
                errors.Add($"Марка {mark} не содержит КИ");
                continue;
            }

            candidates.Add(new GisMtJournalLine
            {
                Mark = mark,
                Cis = cis,
                Gtin = gtin
            });
        }

        return (UniqueByCis(candidates, line => line.Cis), errors);
    }

    /// <summary>
    /// Оставляет по одной строке на код: короткий вариант побеждает, продолжения и точные повторы отбрасываются.
    /// </summary>
    private static List<T> UniqueByCis<T>(IEnumerable<T> items, Func<T, string> cis)
    {
        var list = items.ToList();
        var kept = GisMtCisUniqueness.Distinct(list.Select(cis))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return list
            .Where(item => kept.Contains(cis(item)) && seen.Add(cis(item)))
            .ToList();
    }

    private static string MarkId(GisMtMarkEntity mark) =>
        !string.IsNullOrWhiteSpace(mark.SGtin) ? mark.SGtin : mark.Id;

    private static void ReuseStoredLines(GisMtJournalEntity document, List<GisMtReceiptLinePreview> preview)
    {
        for (var index = 0; index < preview.Count; index++)
        {
            var item = preview[index];

            if (item.Cis.Length > 0)
                continue;

            var stored = document.Lines.FirstOrDefault(line =>
                line.Mark == item.Mark || line.Cis == item.Mark);

            if (stored is null || string.IsNullOrWhiteSpace(stored.Cis))
                continue;

            preview[index] = item with
            {
                Cis = stored.Cis,
                Gtin = stored.Gtin,
                ProductName = stored.ProductName,
                QuantityKg = stored.QuantityKg,
                PricePerKg = stored.PricePerKg,
                AmountKop = stored.AmountKop,
                Error = string.Empty
            };
        }
    }

    private static List<GisMtJournalLine> ToLines(List<GisMtReceiptLinePreview> preview)
    {
        var candidates = new List<GisMtJournalLine>();

        foreach (var item in preview.Where(item => item.Cis.Length > 0))
        {
            var cis = GisMtCisNormalizer.ToCis(item.Cis);

            if (cis.Length == 0)
                continue;

            candidates.Add(new GisMtJournalLine
            {
                Mark = item.Mark,
                Cis = cis,
                ProductName = item.ProductName,
                Gtin = item.Gtin,
                PricePerKg = item.PricePerKg,
                QuantityKg = item.QuantityKg,
                AmountKop = item.AmountKop
            });
        }

        return UniqueByCis(candidates, line => line.Cis);
    }

    private async Task<List<GisMtReceiptLinePreview>> BuildPreview(
        string inn,
        Parameters parameters,
        IReadOnlyList<string> marks,
        CancellationToken cancellationToken)
    {
        var preview = new List<GisMtReceiptLinePreview>();
        var cises = new List<string>();

        foreach (var mark in marks ?? [])
        {
            var value = (mark ?? string.Empty).Trim();
            var cis = _markParser.CalculateCis(value);

            if (cis.Length == 0)
                cis = _markParser.CalculateCis(_markParser.EncodeMark(value));

            if (cis.Length == 0)
            {
                preview.Add(new GisMtReceiptLinePreview(
                    value,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    null,
                    null,
                    null,
                    $"Марка {value} не разбирается в КИ"));

                continue;
            }

            cises.Add(cis);
            preview.Add(new GisMtReceiptLinePreview(
                value,
                cis,
                GisMtCisGtin.Extract(cis),
                string.Empty,
                null,
                null,
                null,
                string.Empty));
        }

        if (cises.Count == 0)
            return preview;

        var details = await LoadCisDetails(inn, cises, cancellationToken);
        var prices = await LoadPrices(cises, parameters);

        for (var index = 0; index < preview.Count; index++)
        {
            var item = preview[index];

            if (item.Cis.Length == 0)
                continue;

            details.TryGetValue(item.Cis, out var detail);
            prices.TryGetValue(item.Gtin, out var price);

            var quantityKg = detail?.QuantityKg;
            var pricePerKg = price > 0 ? price : (decimal?)null;

            preview[index] = item with
            {
                ProductName = detail?.ProductName ?? string.Empty,
                QuantityKg = quantityKg,
                PricePerKg = pricePerKg,
                AmountKop = GisMtReceiptAmount.Calculate(pricePerKg, quantityKg),
                Error = MissingDataWarning(quantityKg, pricePerKg)
            };
        }

        return preview;
    }

    private static string MissingDataWarning(decimal? quantityKg, decimal? pricePerKg)
    {
        if (quantityKg is null)
            return "Количество не получено из ГИС МТ";

        if (pricePerKg is null)
            return "Цена не найдена в базе Frontol";

        return string.Empty;
    }

    private async Task<Dictionary<string, CisDetails>> LoadCisDetails(string inn, List<string> cises, CancellationToken cancellationToken)
    {
        var details = new Dictionary<string, CisDetails>(StringComparer.OrdinalIgnoreCase);
        var token = _applicationState.TrueApiToken(inn).Token;

        if (string.IsNullOrWhiteSpace(token))
            return details;

        var info = await _cisesClient.CisesInfo(token, cises, MilkProductGroup, cancellationToken);

        if (info.IsFailure)
        {
            _logger.LogWarning("Количество и название строк списания не получены: {Error}", info.Error);
            return details;
        }

        foreach (var item in info.Value)
        {
            var cis = item.CisInfo?.Cis ?? item.CisInfo?.RequestedCis;

            if (string.IsNullOrWhiteSpace(cis))
                continue;

            details[cis] = new CisDetails(ToKilograms(item.CisInfo), item.CisInfo?.ProductName ?? string.Empty);
        }

        await FillFromProductInfo(token, cises, details, cancellationToken);

        return details;
    }

    private async Task FillFromProductInfo(
        string token,
        List<string> cises,
        Dictionary<string, CisDetails> details,
        CancellationToken cancellationToken)
    {
        var incomplete = cises
            .Where(cis => !details.TryGetValue(cis, out var detail)
                          || detail.QuantityKg is null
                          || string.IsNullOrWhiteSpace(detail.ProductName))
            .ToList();

        if (incomplete.Count == 0)
            return;

        var gtins = incomplete
            .Select(GisMtCisGtin.Extract)
            .Where(gtin => gtin.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (gtins.Count == 0)
            return;

        var products = await _cisesClient.ProductInfo(token, gtins, cancellationToken);

        if (products.IsFailure)
        {
            _logger.LogWarning("Карточки товаров для строк списания не получены: {Error}", products.Error);
            return;
        }

        var byGtin = products.Value.Items
            .Where(product => !string.IsNullOrWhiteSpace(product.Gtin))
            .GroupBy(product => product.Gtin, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var cis in incomplete)
        {
            var gtin = GisMtCisGtin.Extract(cis);

            if (gtin.Length == 0 || !byGtin.TryGetValue(gtin, out var product))
                continue;

            details.TryGetValue(cis, out var current);

            var quantity = current?.QuantityKg;

            if (quantity is null && product.ProductWeight is > 0)
                quantity = Math.Round((decimal)product.ProductWeight.Value, 3, MidpointRounding.AwayFromZero);

            var name = string.IsNullOrWhiteSpace(current?.ProductName) ? product.Name : current!.ProductName;

            details[cis] = new CisDetails(quantity, name ?? string.Empty);
        }
    }

    private static decimal? ToKilograms(CisInfoData? info)
    {
        if (info is null)
            return null;

        // productWeightGr — граммы, productWeight — килограммы.
        if (info.ProductWeightGr is > 0)
            return Math.Round(info.ProductWeightGr.Value / 1000m, 3, MidpointRounding.AwayFromZero);

        if (info.ProductWeight is > 0)
            return Math.Round((decimal)info.ProductWeight.Value, 3, MidpointRounding.AwayFromZero);

        return null;
    }

    private sealed record CisDetails(decimal? QuantityKg, string ProductName);

    private async Task<Dictionary<string, decimal>> LoadPrices(List<string> cises, Parameters parameters)
    {
        var prices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var connectionId = parameters.ConnectedFrontolSettings.ResolveWareDataSourceId();

        if (connectionId == 0)
            return prices;

        var connection = parameters.ConnectedFrontolSettings.ConnectionSettings
            .FirstOrDefault(item => item.Id == connectionId);

        if (connection == null || !connection.ConnectionEnable())
            return prices;

        using var sprt = _sprtFactory.Create(connection.ConnectionStringBuild());

        var gtins = cises
            .Select(GisMtCisGtin.Extract)
            .Where(gtin => gtin.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var gtin in gtins)
        {
            var price = await sprt.RetailPriceByBarcode(gtin);

            if (price.IsFailure)
            {
                _logger.LogWarning("Цена товара {Gtin} из базы Frontol не прочитана: {Error}", gtin, price.Error);
                continue;
            }

            if (price.Value is null)
                continue;

            prices[gtin] = price.Value.Value;
        }

        return prices;
    }
}
