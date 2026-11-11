using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Journal;
using FmuApiDomain.GisMt.Models;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.GisMt.Journal;

/// <summary>
/// Журнал исходящих документов ГИС МТ: идемпотентное создание по uid и отправка в Честный знак.
/// </summary>
[AutoRegisterService(ServiceLifetime.Scoped)]
public class GisMtJournalService : IGisMtJournalService
{
    private const string BeerProductGroup = "beer";
    private const string MilkProductGroup = "milk";
    private const int StatusCheckCooldownMinutes = 1;
    private const int StatusCheckMaxAgeDays = 1;
    private const int PendingStatusLimit = 50;

    private readonly ILogger<GisMtJournalService> _logger;
    private readonly IGisMtJournalRepository _repository;
    private readonly IParametersService _parametersService;
    private readonly IApplicationState _applicationState;
    private readonly IGisMtDocumentSigner _signer;
    private readonly IGisMtLkDocumentsClient _client;
    private readonly IGisMtMarkRepository _markRepository;
    private readonly IGisMtDocumentsClient _documentsClient;
    private readonly IGisMtCisHistoryClient _cisHistoryClient;

    public GisMtJournalService(
        ILogger<GisMtJournalService> logger,
        IGisMtJournalRepository repository,
        IParametersService parametersService,
        IApplicationState applicationState,
        IGisMtDocumentSigner signer,
        IGisMtLkDocumentsClient client,
        IGisMtMarkRepository markRepository,
        IGisMtDocumentsClient documentsClient,
        IGisMtCisHistoryClient cisHistoryClient)
    {
        _logger = logger;
        _repository = repository;
        _parametersService = parametersService;
        _applicationState = applicationState;
        _signer = signer;
        _client = client;
        _markRepository = markRepository;
        _documentsClient = documentsClient;
        _cisHistoryClient = cisHistoryClient;
    }

    public async Task<Result<GisMtJournalPage>> List(string kind, string status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1)
            page = 1;

        if (pageSize is < 1 or > 200)
            pageSize = 50;

        return await _repository.List(kind, status, page, pageSize);
    }

    public async Task<GisMtJournalEntity?> Get(string id) => await _repository.Get(id);

    public async Task<Result<GisMtJournalEntity>> Create(GisMtJournalEntity document)
    {
        var uid = (document.Uid ?? string.Empty).Trim();

        if (uid.Length > 0)
        {
            document.Uid = uid;
            // Id выводится из uid: в CouchDB уникальность поля не держит индекс,
            // поэтому два параллельных вызова пишут один и тот же документ, а не два.
            document.Id = GisMtJournalIds.FromUid(uid);

            var existing = await _repository.Get(document.Id);

            // Акты, созданные до перехода на id из uid, находим по самому uid.
            existing ??= await _repository.FindByUid(uid);

            if (existing is not null)
            {
                _logger.LogInformation("Акт ГИС МТ по uid {Uid} уже есть: {Id}", uid, existing.Id);
                return Result.Success(existing);
            }
        }

        if (string.IsNullOrEmpty(document.Id))
            document.Id = Guid.NewGuid().ToString();

        if (!await _repository.Save(document))
        {
            var existing = uid.Length == 0
                ? null
                : await _repository.Get(document.Id) ?? await _repository.FindByUid(uid);

            if (existing is not null)
                return Result.Success(existing);

            return Result.Failure<GisMtJournalEntity>("Не удалось сохранить акт журнала ГИС МТ");
        }

        return Result.Success(document);
    }

    public async Task<Result<GisMtJournalEntity>> Send(string id, CancellationToken cancellationToken = default)
    {
        var document = await _repository.Get(id);

        if (document is null)
            return Result.Failure<GisMtJournalEntity>("Акт журнала ГИС МТ не найден");

        if (document.Status == GisMtJournalStatuses.Sent)
            return Result.Failure<GisMtJournalEntity>("Акт уже отправлен");

        if (GisMtJournalStatuses.IsLocked(document.Status))
            return Result.Failure<GisMtJournalEntity>("Документ уже отправляется");

        if (document.Lines.Count == 0)
            return Result.Failure<GisMtJournalEntity>("В документе нет ни одной марки");

        var parameters = await _parametersService.CurrentAsync();
        var organisation = GisMtOrganisationResolver.Find(parameters, document.Inn);

        if (organisation is null)
            return await Fail(document, $"Организация с ИНН {document.Inn} не найдена");

        var token = _applicationState.TrueApiToken(organisation.INN).Token;

        if (string.IsNullOrWhiteSpace(token))
            return await Fail(document, $"Нет Bearer-токена для ИНН {organisation.INN}");

        var participant = GisMtParticipantRules.Resolve(
            document.Inn,
            organisation.Kpp,
            organisation.FiasId,
            GisMtModRequirement.Required(document.Kind, document.Reason));

        if (participant.IsFailure)
            return await Fail(document, participant.Error);

        var duplicates = document.Lines
            .GroupBy(line => (line.Cis ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Take(5)
            .ToList();

        if (duplicates.Count > 0)
            return await Fail(document, $"В документе повторяются КИ: {string.Join(", ", duplicates)}");

        string body;
        Result<string> signature;

        try
        {
            body = document.Kind == GisMtJournalKinds.ConnectTap
                ? GisMtConnectTapBody.Build(document, participant.Value)
                : GisMtLkReceiptBody.Build(document, participant.Value);

            document.CrptBody = body;

            signature = _signer.SignDetached(
                organisation.INN,
                organisation.TrueApiIntegrationSettings.DigitalSignature,
                organisation.TrueApiIntegrationSettings.Password,
                body);
        }
        catch (Exception ex)
        {
            return await Fail(document, ex.Message);
        }

        if (signature.IsFailure)
            return await Fail(document, signature.Error);

        document.Status = GisMtJournalStatuses.Sending;
        document.ErrorText = string.Empty;

        // Статус занимается условно: только у акта в draft или error. Иначе второй параллельный
        // вызов тоже прошёл бы проверки выше и отправил второй документ в Честный знак.
        var claimed = await _repository.TryUpdateStatus(document, GisMtJournalStatuses.Draft)
            || await _repository.TryUpdateStatus(document, GisMtJournalStatuses.Error);

        if (!claimed)
        {
            var current = await _repository.Get(document.Id);

            if (current?.Status is GisMtJournalStatuses.Sent or GisMtJournalStatuses.Sending)
                return Result.Failure<GisMtJournalEntity>("Документ уже отправляется или отправлен");

            _logger.LogError("Акт ГИС МТ {Id}: не удалось сохранить статус отправки", document.Id);

            return Result.Failure<GisMtJournalEntity>("Не удалось сохранить статус отправки акта журнала ГИС МТ");
        }

        Result<string> created;

        try
        {
            created = await _client.CreateDocument(
                token,
                ResolveProductGroup(document),
                document.Kind,
                body,
                signature.Value,
                cancellationToken);
        }
        catch (Exception ex)
        {
            return await Unconfirmed(document, ex);
        }

        if (created.IsFailure)
            return await Fail(document, created.Error);

        document.Status = GisMtJournalStatuses.Sent;
        document.SentAt = DateTime.Now;
        document.CrptDocumentId = created.Value;
        document.ErrorText = string.Empty;

        if (!await _repository.Save(document))
        {
            _logger.LogError(
                "Документ ГИС МТ {Id} создан в Честном знаке как {CrptDocumentId}, но статус и идентификатор в акте не сохранены",
                document.Id,
                created.Value);

            return Result.Failure<GisMtJournalEntity>(
                $"Документ создан в Честном знаке ({created.Value}), но в акт журнала это не записано: статус не сохранён");
        }

        await MarkExpiredSold(document);

        return Result.Success(document);
    }

    /// <summary>
    /// Отправка прервалась после запроса в Честный знак: документ мог быть принят.
    /// Ошибку не ставим, иначе акт можно отправить повторно и получить второй документ.
    /// </summary>
    private async Task<Result<GisMtJournalEntity>> Unconfirmed(GisMtJournalEntity document, Exception exception)
    {
        var reason = exception is OperationCanceledException
            ? "истекло время ожидания ответа"
            : exception.Message;

        document.Status = GisMtJournalStatuses.Sending;
        document.ErrorText =
            $"Ответ Честного знака не получен ({reason}): документ мог быть принят, проверьте его в личном кабинете";

        if (!await _repository.Save(document))
            _logger.LogError(
                "Акт ГИС МТ {Id}: не удалось сохранить причину неподтверждённой отправки",
                document.Id);

        _logger.LogWarning(
            "Акт ГИС МТ {Id}: отправка не подтверждена, идентификатор документа Честного знака не получен: {Error}",
            document.Id,
            exception.Message);

        return Result.Failure<GisMtJournalEntity>(document.ErrorText);
    }

    public async Task<Result<GisMtJournalEntity>> ReconcileUnconfirmed(
        string id,
        string crptDocumentId,
        CancellationToken cancellationToken = default)
    {
        var document = await _repository.Get(id);

        if (document is null)
            return Result.Failure<GisMtJournalEntity>("Акт журнала ГИС МТ не найден");

        if (!IsUnconfirmed(document))
            return Result.Failure<GisMtJournalEntity>("Сверять можно только акт с неподтверждённой отправкой");

        var value = (crptDocumentId ?? string.Empty).Trim();

        if (value.Length == 0)
            return Result.Failure<GisMtJournalEntity>("Укажите идентификатор документа Честного знака");

        var parameters = await _parametersService.CurrentAsync();
        var organisation = GisMtOrganisationResolver.Find(parameters, document.Inn);

        if (organisation is null)
            return Result.Failure<GisMtJournalEntity>($"Организация с ИНН {document.Inn} не найдена");

        var token = _applicationState.TrueApiToken(organisation.INN).Token;

        if (string.IsNullOrWhiteSpace(token))
            return Result.Failure<GisMtJournalEntity>($"Нет Bearer-токена для ИНН {organisation.INN}");

        // Акт считается сверенным, только если Честный знак знает такой документ.
        var info = await _documentsClient.DocumentInfo(token, value, null, cancellationToken);

        if (info.IsFailure)
            return Result.Failure<GisMtJournalEntity>($"Документ {value} в Честном знаке не найден: {info.Error}");

        document.Status = GisMtJournalStatuses.Sent;
        document.CrptDocumentId = value;
        document.SentAt ??= DateTime.Now;
        document.ErrorText = string.Empty;
        document.CrptStatus = (info.Value.Status ?? string.Empty).Trim().ToUpperInvariant();
        document.CrptStatusText = info.Value.ErrorMessages.Count == 0
            ? string.Empty
            : string.Join("; ", info.Value.ErrorMessages);
        document.CrptStatusCheckedAt = DateTime.Now;

        if (!await _repository.Save(document))
            return Result.Failure<GisMtJournalEntity>("Не удалось сохранить акт журнала ГИС МТ");

        // Обычная отправка помечает марки проданными сразу; после сверки делаем то же,
        // если Честный знак уже не отказал — иначе возвращать в остаток нечего.
        if (!GisMtCrptStatuses.IsFailed(document.CrptStatus))
            await MarkExpiredSold(document);

        _logger.LogInformation(
            "Акт ГИС МТ {Id} сверен с документом Честного знака {DocumentId}",
            document.Id,
            value);

        return Result.Success(document);
    }

    public async Task<Result<GisMtJournalEntity>> ReturnUnconfirmedToError(string id)
    {
        var document = await _repository.Get(id);

        if (document is null)
            return Result.Failure<GisMtJournalEntity>("Акт журнала ГИС МТ не найден");

        if (!IsUnconfirmed(document))
            return Result.Failure<GisMtJournalEntity>("Вернуть в ошибку можно только акт с неподтверждённой отправкой");

        document.Status = GisMtJournalStatuses.Error;
        document.ErrorText = "Документ в Честном знаке не найден: отправку можно повторить";

        if (!await _repository.Save(document))
            return Result.Failure<GisMtJournalEntity>("Не удалось сохранить акт журнала ГИС МТ");

        _logger.LogWarning("Акт ГИС МТ {Id}: документа в Честном знаке нет, акт возвращён в ошибку", document.Id);

        return Result.Success(document);
    }

    /// <summary>
    /// Акт отправлен, но ответ Честного знака не получен: ни идентификатора, ни ошибки у него нет.
    /// </summary>
    private static bool IsUnconfirmed(GisMtJournalEntity document) =>
        document.Status == GisMtJournalStatuses.Sending
        && string.IsNullOrWhiteSpace(document.CrptDocumentId);

    public async Task<Result<int>> CheckPendingCrptStatuses(CancellationToken cancellationToken = default)
    {
        var parameters = await _parametersService.CurrentAsync();

        if (!parameters.GisMtSettings.DocumentsEnabled)
            return Result.Success(0);

        var documents = await _repository.GetPendingCrptStatus(
            DateTime.Now.AddMinutes(-StatusCheckCooldownMinutes),
            DateTime.Now.AddDays(-StatusCheckMaxAgeDays),
            PendingStatusLimit);

        var checkedCount = 0;

        foreach (var document in documents)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var result = await CheckCrptStatus(document.Id, cancellationToken);

                if (result.IsSuccess)
                {
                    checkedCount++;
                    continue;
                }

                _logger.LogWarning("Статус документа ГИС МТ {Id} не получен: {Error}", document.Id, result.Error);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (OperationCanceledException ex)
            {
                // Таймаут HttpClient: остальные акты опрашиваем дальше, отказ Честного знака ещё придёт.
                _logger.LogWarning("Статус документа ГИС МТ {Id} не получен по таймауту: {Error}", document.Id, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка опроса статуса документа ГИС МТ {Id}", document.Id);
            }
        }

        return Result.Success(checkedCount);
    }

    private static string ResolveProductGroup(GisMtJournalEntity document)
    {
        if (document.Kind == GisMtJournalKinds.ConnectTap)
            return BeerProductGroup;

        var group = (document.ProductGroup ?? string.Empty).Trim();

        return group.Length == 0 ? MilkProductGroup : group;
    }

    private async Task DescribeRejectedCodes(GisMtJournalEntity document, string token, CancellationToken cancellationToken)
    {
        var codes = RejectedCodes(document.CrptStatusText);

        if (codes.Count == 0)
            return;

        var rows = new List<string>();

        foreach (var cis in codes)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var history = await _cisHistoryClient.History(token, cis, cancellationToken);

            if (history.IsFailure || history.Value.Count == 0)
            {
                rows.Add($"{cis}: сведения о коде не получены");
                continue;
            }

            var record = history.Value[0];
            var parent = string.IsNullOrWhiteSpace(record.Parent)
                ? "без упаковки"
                : $"в упаковке {record.Parent}";

            rows.Add($"{cis}: статус {record.Status}, {record.PackageType}, {parent}");
        }

        document.CrptErrorsDetail = string.Join("; ", rows);
        await _repository.Save(document);

        _logger.LogInformation(
            "Документ ГИС МТ {Id}: разобрано отклонённых кодов {Count}",
            document.Id,
            rows.Count);
    }

    /// <summary>
    /// Коды из текста ошибок ГИС МТ вида «16: Код идентификации &lt;код&gt; не уникален в документе».
    /// </summary>
    private static List<string> RejectedCodes(string? text)
    {
        const string marker = "Код идентификации ";

        return (text ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Trim())
            .Select(part =>
            {
                var start = part.IndexOf(marker, StringComparison.Ordinal);

                if (start < 0)
                    return string.Empty;

                var rest = part[(start + marker.Length)..];
                var end = rest.IndexOf(" не ", StringComparison.Ordinal);

                return end < 0 ? string.Empty : rest[..end].Trim();
            })
            .Where(code => code.StartsWith("01", StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToList();
    }

    private async Task ResetExpiredSold(GisMtJournalEntity document)
    {
        if (document.Kind != GisMtJournalKinds.LkReceipt || !GisMtWriteOffReasons.IsExpiration(document.Reason))
            return;

        var failed = 0;

        foreach (var line in document.Lines)
        {
            var markId = (line.Mark ?? string.Empty).Trim();

            if (markId.Length == 0 || (await _markRepository.ChangeState(markId, false)).IsFailure)
                failed++;
        }

        if (failed > 0)
            _logger.LogWarning(
                "Марки списания по сроку годности {Id} не возвращены в остатки: ошибок {Failed} из {Total}",
                document.Id,
                failed,
                document.Lines.Count);
    }

    private async Task MarkExpiredSold(GisMtJournalEntity document)
    {
        if (document.Kind != GisMtJournalKinds.LkReceipt || !GisMtWriteOffReasons.IsExpiration(document.Reason))
            return;

        var failed = 0;

        foreach (var line in document.Lines)
        {
            var markId = (line.Mark ?? string.Empty).Trim();

            if (markId.Length == 0 || (await _markRepository.ChangeState(markId, true)).IsFailure)
                failed++;
        }

        if (failed > 0)
            _logger.LogWarning(
                "Марки списания по сроку годности {Id} помечены проданными не все: ошибок {Failed} из {Total}",
                document.Id,
                failed,
                document.Lines.Count);
    }

    public async Task<Result<GisMtJournalEntity>> CheckCrptStatus(string id, CancellationToken cancellationToken = default)
    {
        var document = await _repository.Get(id);

        if (document is null)
            return Result.Failure<GisMtJournalEntity>("Акт журнала ГИС МТ не найден");

        if (string.IsNullOrWhiteSpace(document.CrptDocumentId))
            return Result.Failure<GisMtJournalEntity>("У акта нет идентификатора документа Честного знака");

        var parameters = await _parametersService.CurrentAsync();
        var organisation = GisMtOrganisationResolver.Find(parameters, document.Inn);

        if (organisation is null)
            return Result.Failure<GisMtJournalEntity>($"Организация с ИНН {document.Inn} не найдена");

        var token = _applicationState.TrueApiToken(organisation.INN).Token;

        if (string.IsNullOrWhiteSpace(token))
            return Result.Failure<GisMtJournalEntity>($"Нет Bearer-токена для ИНН {organisation.INN}");

        var info = await _documentsClient.DocumentInfo(token, document.CrptDocumentId, null, cancellationToken);

        if (info.IsFailure)
            return Result.Failure<GisMtJournalEntity>(info.Error);

        document.CrptStatus = (info.Value.Status ?? string.Empty).Trim().ToUpperInvariant();
        document.CrptStatusText = info.Value.ErrorMessages.Count == 0
            ? string.Empty
            : string.Join("; ", info.Value.ErrorMessages);
        document.CrptStatusCheckedAt = DateTime.Now;

        if (!await _repository.Save(document))
            return Result.Failure<GisMtJournalEntity>("Не удалось сохранить статус акта журнала ГИС МТ");

        if (GisMtCrptStatuses.IsFailed(document.CrptStatus))
        {
            await ResetExpiredSold(document);
            await DescribeRejectedCodes(document, token, cancellationToken);
        }

        _logger.LogInformation(
            "Статус документа ГИС МТ {DocumentId}: {Status}",
            document.CrptDocumentId,
            document.CrptStatus);

        return Result.Success(document);
    }

    public async Task<Result> Delete(string id)
    {
        var document = await _repository.Get(id);

        if (document is null)
            return Result.Failure("Акт журнала ГИС МТ не найден");

        if (document.Status == GisMtJournalStatuses.Sent && !GisMtCrptStatuses.IsFailed(document.CrptStatus))
            return Result.Failure("Отправленный документ удалить нельзя");

        if (GisMtJournalStatuses.IsLocked(document.Status))
            return Result.Failure("Документ отправляется, удалить его нельзя");

        if (!await _repository.Delete(id))
            return Result.Failure("Не удалось удалить акт журнала ГИС МТ");

        _logger.LogInformation("Акт ГИС МТ {Id} удалён из журнала", id);

        return Result.Success();
    }

    private async Task<Result<GisMtJournalEntity>> Fail(GisMtJournalEntity document, string error)
    {
        document.Status = GisMtJournalStatuses.Error;
        document.ErrorText = error;

        if (!await _repository.Save(document))
        {
            _logger.LogError(
                "Акт ГИС МТ {Id} не отправлен ({Error}), но статус ошибки не сохранён",
                document.Id,
                error);

            return Result.Failure<GisMtJournalEntity>("Не удалось сохранить ошибку отправки акта журнала ГИС МТ");
        }

        _logger.LogWarning("Акт ГИС МТ {Id} не отправлен: {Error}", document.Id, error);

        return Result.Failure<GisMtJournalEntity>(error);
    }
}
