using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Journal;
using FmuApiDomain.GisMt.Models;
using FmuApiDomain.State.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.TrueSign;

[Route("api/ts/gismt/journal")]
[ApiController]
[ApiExplorerSettings(GroupName = "True API")]
public class GisMtJournalController : ControllerBase
{
    private readonly IGisMtJournalService _journalService;
    private readonly IGisMtReceiptService _receiptService;
    private readonly IParametersService _parametersService;
    private readonly IApplicationState _applicationState;

    public GisMtJournalController(
        IGisMtJournalService journalService,
        IGisMtReceiptService receiptService,
        IParametersService parametersService,
        IApplicationState applicationState)
    {
        _journalService = journalService;
        _receiptService = receiptService;
        _parametersService = parametersService;
        _applicationState = applicationState;
    }

    /// <summary>
    /// Страница журнала документов ГИС МТ.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? kind,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var check = await CheckDatabase();

        if (check != null)
            return check;

        var result = await _journalService.List(kind ?? string.Empty, status ?? string.Empty, page, pageSize, cancellationToken);

        if (result.IsFailure)
            return StatusCode(500, new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Карточка акта журнала.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var check = await CheckDatabase();

        if (check != null)
            return check;

        var document = await _journalService.Get(id);

        if (document is null)
            return NotFound(new { error = "Акт журнала ГИС МТ не найден" });

        return Ok(document);
    }

    /// <summary>
    /// Считает КИ, вес, цену и сумму строк по маркам до сохранения документа.
    /// </summary>
    [HttpPost("receipt/preview")]
    public async Task<IActionResult> PreviewReceipt([FromBody] CreateReceiptRequest request, CancellationToken cancellationToken)
    {
        if (request?.Marks is null || request.Marks.Count == 0)
            return BadRequest(new { error = "В документе нет ни одной марки" });

        var result = await _receiptService.Preview(request.Inn ?? string.Empty, request.Marks, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Создаёт черновик списания молочной продукции из марок.
    /// </summary>
    [HttpPost("receipt")]
    public async Task<IActionResult> CreateReceipt([FromBody] CreateReceiptRequest request, CancellationToken cancellationToken)
    {
        var check = await CheckDatabase();

        if (check != null)
            return check;

        if (request?.Marks is null || request.Marks.Count == 0)
            return BadRequest(new { error = "В документе нет ни одной марки" });

        var result = await _receiptService.Create(request.Inn ?? string.Empty, request.Marks, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(new
        {
            document = result.Value.Document,
            lineErrors = result.Value.LineErrors
        });
    }

    /// <summary>
    /// Товарные группы организации, по которым есть остатки марок.
    /// </summary>
    [HttpGet("receipt/groups")]
    public async Task<IActionResult> Groups([FromQuery] string? inn, CancellationToken cancellationToken)
    {
        var check = await CheckDatabase();

        if (check != null)
            return check;

        var result = await _receiptService.Groups(inn ?? string.Empty, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Сколько просроченных непроданных марок в товарной группе.
    /// </summary>
    [HttpGet("receipt/expired-count")]
    public async Task<IActionResult> ExpiredCount(
        [FromQuery] string? inn,
        [FromQuery] string? productGroup,
        CancellationToken cancellationToken)
    {
        var check = await CheckDatabase();

        if (check != null)
            return check;

        var result = await _receiptService.ExpiredCount(inn ?? string.Empty, productGroup ?? string.Empty, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(new { count = result.Value });
    }

    /// <summary>
    /// Подбирает просроченные марки одной товарной группы для списания по сроку годности.
    /// </summary>
    [HttpPost("receipt/expired")]
    public async Task<IActionResult> FillExpired([FromBody] FillExpiredRequest request, CancellationToken cancellationToken)
    {
        var check = await CheckDatabase();

        if (check != null)
            return check;

        var result = await _receiptService.FillExpired(
            request?.Inn ?? string.Empty,
            request?.ProductGroup ?? string.Empty,
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(new
        {
            marks = result.Value.Marks,
            excludedPackages = result.Value.ExcludedPackages
        });
    }

    /// <summary>
    /// Создаёт черновик списания по истечении срока годности из просроченных марок.
    /// </summary>
    [HttpPost("receipt/expiration")]
    public async Task<IActionResult> CreateExpirationReceipt([FromBody] CreateExpiredReceiptRequest request, CancellationToken cancellationToken)
    {
        var check = await CheckDatabase();

        if (check != null)
            return check;

        if (request?.Lines is null || request.Lines.Count == 0)
            return BadRequest(new { error = "В документе нет ни одной марки" });

        var result = await _receiptService.CreateExpiration(
            request.Inn ?? string.Empty,
            request.ProductGroup ?? string.Empty,
            ToExpiredLines(request.Lines),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(new
        {
            document = result.Value.Document,
            lineErrors = result.Value.LineErrors
        });
    }

    /// <summary>
    /// Изменяет черновик списания по истечении срока годности.
    /// </summary>
    [HttpPut("{id}/receipt/expiration")]
    public async Task<IActionResult> UpdateExpirationReceipt(string id, [FromBody] CreateExpiredReceiptRequest request, CancellationToken cancellationToken)
    {
        var check = await CheckDatabase();

        if (check != null)
            return check;

        if (request?.Lines is null || request.Lines.Count == 0)
            return BadRequest(new { error = "В документе нет ни одной марки" });

        var result = await _receiptService.UpdateExpiration(
            id,
            request.Inn ?? string.Empty,
            request.ProductGroup ?? string.Empty,
            ToExpiredLines(request.Lines),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(new
        {
            document = result.Value.Document,
            lineErrors = result.Value.LineErrors
        });
    }

    /// <summary>
    /// Запрашивает в Честном знаке статус обработки отправленного документа.
    /// </summary>
    [HttpPost("{id}/status")]
    public async Task<IActionResult> CrptStatus(string id, CancellationToken cancellationToken)
    {
        var check = await CheckDatabase();

        if (check != null)
            return check;

        var result = await _journalService.CheckCrptStatus(id, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(new
        {
            document = result.Value,
            status = result.Value.CrptStatus,
            statusText = GisMtCrptStatuses.Text(result.Value.CrptStatus),
            errors = result.Value.CrptStatusText
        });
    }

    /// <summary>
    /// Отправляет акт в Честный знак.
    /// </summary>
    [HttpPost("{id}/send")]
    public async Task<IActionResult> Send(string id, CancellationToken cancellationToken)
    {
        var check = await CheckDatabase();

        if (check != null)
            return check;

        var result = await _journalService.Send(id, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Сверяет акт с неподтверждённой отправкой: привязывает найденный в Честном знаке документ.
    /// </summary>
    [HttpPost("{id}/reconcile")]
    public async Task<IActionResult> Reconcile(string id, [FromBody] ReconcileRequest request, CancellationToken cancellationToken)
    {
        var check = await CheckDatabase();

        if (check != null)
            return check;

        var result = await _journalService.ReconcileUnconfirmed(
            id,
            request?.CrptDocumentId ?? string.Empty,
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Возвращает акт в ошибку, когда документа в Честном знаке нет: отправку можно повторить.
    /// </summary>
    [HttpPost("{id}/unconfirmed/not-found")]
    public async Task<IActionResult> UnconfirmedNotFound(string id)
    {
        var check = await CheckDatabase();

        if (check != null)
            return check;

        var result = await _journalService.ReturnUnconfirmedToError(id);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Изменяет черновик списания молочной продукции.
    /// </summary>
    [HttpPut("{id}/receipt")]
    public async Task<IActionResult> UpdateReceipt(string id, [FromBody] CreateReceiptRequest request, CancellationToken cancellationToken)
    {
        var check = await CheckDatabase();

        if (check != null)
            return check;

        if (request?.Marks is null || request.Marks.Count == 0)
            return BadRequest(new { error = "В документе нет ни одной марки" });

        var result = await _receiptService.Update(id, request.Inn ?? string.Empty, request.Marks, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(new
        {
            document = result.Value.Document,
            lineErrors = result.Value.LineErrors
        });
    }

    /// <summary>
    /// Удаляет акт журнала, который ещё не отправлен.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var check = await CheckDatabase();

        if (check != null)
            return check;

        var result = await _journalService.Delete(id);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok();
    }

    private async Task<IActionResult?> CheckDatabase()
    {
        var settings = await _parametersService.CurrentAsync();

        if (!settings.Database.Enable)
            return BadRequest(new { error = "База данных отключена" });

        if (!_applicationState.CouchDbOnline())
            return BadRequest(new { error = "База данных недоступна в данный момент" });

        return null;
    }

    private static List<GisMtExpiredLine> ToExpiredLines(IEnumerable<ExpiredLineRequest> lines) => lines
        .Select(line => new GisMtExpiredLine(line.Mark ?? string.Empty, line.Cis ?? string.Empty))
        .ToList();

    public record CreateReceiptRequest(string? Inn, List<string>? Marks);

    public record ReconcileRequest(string? CrptDocumentId);

    public record CreateExpiredReceiptRequest(string? Inn, string? ProductGroup, List<ExpiredLineRequest>? Lines);

    public record ExpiredLineRequest(string? Mark, string? Cis);

    public record FillExpiredRequest(string? Inn, string? ProductGroup);
}
