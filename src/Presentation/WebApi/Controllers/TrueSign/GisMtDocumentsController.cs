using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.State.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.TrueSign;

[Route("api/ts/gismt")]
[ApiController]
[ApiExplorerSettings(GroupName = "True API")]
public class GisMtDocumentsController : ControllerBase
{
    private const int DocumentMarksLimit = 1000;

    private readonly IGisMtDocumentsSyncService _documentsSyncService;
    private readonly IGisMtProductGroupsService _productGroupsService;
    private readonly IGisMtStockLoadService _stockLoadService;
    private readonly IGisMtMarkRepository _markRepository;
    private readonly IGisMtDocumentRepository _documentRepository;
    private readonly IParametersService _parametersService;
    private readonly IApplicationState _appState;

    public GisMtDocumentsController(
        IGisMtDocumentsSyncService documentsSyncService,
        IGisMtProductGroupsService productGroupsService,
        IGisMtStockLoadService stockLoadService,
        IGisMtMarkRepository markRepository,
        IGisMtDocumentRepository documentRepository,
        IParametersService parametersService,
        IApplicationState applicationState)
    {
        _documentsSyncService = documentsSyncService;
        _productGroupsService = productGroupsService;
        _stockLoadService = stockLoadService;
        _markRepository = markRepository;
        _documentRepository = documentRepository;
        _parametersService = parametersService;
        _appState = applicationState;
    }

    /// <summary>
    /// Ручной запуск синхронизации входящих УПД/УКД за настроенный период.
    /// </summary>
    [HttpPost("sync")]
    public async Task<IActionResult> Sync(CancellationToken cancellationToken)
    {
        var result = await _documentsSyncService.Sync(cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Обновляет список товарных групп организации из ГИС МТ и сохраняет в конфигурацию.
    /// </summary>
    [HttpPost("product-groups/refresh")]
    public async Task<IActionResult> RefreshProductGroups([FromQuery] string inn, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(inn))
            return BadRequest(new { error = "Параметр inn обязателен" });

        var result = await _productGroupsService.Refresh(inn, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(new { inn, productGroups = result.Value });
    }

    /// <summary>
    /// Загружает остаток марок со статусом «В обороте» для организации.
    /// </summary>
    [HttpPost("stock/load")]
    public async Task<IActionResult> LoadStock([FromQuery] string inn, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(inn))
            return BadRequest(new { error = "Параметр inn обязателен" });

        var result = await _stockLoadService.Load(inn, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Загруженный документ УПД/УКД и марки, которые пришли с ним.
    /// </summary>
    [HttpGet("documents/by-number")]
    public async Task<IActionResult> GetDocument([FromQuery] string number)
    {
        var settings = await _parametersService.CurrentAsync();

        if (!settings.Database.Enable)
            return BadRequest(new { error = "База данных отключена" });

        if (!_appState.CouchDbOnline())
            return BadRequest(new { error = "База данных недоступна в данный момент" });

        if (string.IsNullOrWhiteSpace(number))
            return BadRequest(new { error = "Номер документа обязателен" });

        var document = await _documentRepository.Get(number);

        if (document == null)
            return NotFound(new { error = $"Документ {number} не найден" });

        var marks = await _markRepository.GetBySourceDocument(document.Number, DocumentMarksLimit);

        return Ok(new { document, marks });
    }

    /// <summary>
    /// Заново загружает документ ГИС МТ: тело ответа doc/info и марки.
    /// </summary>
    [HttpPost("documents/reload")]
    public async Task<IActionResult> ReloadDocument([FromQuery] string number, CancellationToken cancellationToken)
    {
        var settings = await _parametersService.CurrentAsync();

        if (!settings.Database.Enable)
            return BadRequest(new { error = "База данных отключена" });

        if (!_appState.CouchDbOnline())
            return BadRequest(new { error = "База данных недоступна в данный момент" });

        if (string.IsNullOrWhiteSpace(number))
            return BadRequest(new { error = "Номер документа обязателен" });

        var result = await _documentsSyncService.ReloadDocument(number, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(new { marks = result.Value });
    }

    /// <summary>
    /// Страница загруженных входящих документов УПД/УКД.
    /// </summary>
    [HttpGet("documents")]
    public async Task<IActionResult> GetDocuments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var settings = await _parametersService.CurrentAsync();

        if (!settings.Database.Enable)
            return BadRequest(new { error = "База данных отключена" });

        if (!_appState.CouchDbOnline())
            return BadRequest(new { error = "База данных недоступна в данный момент" });

        if (page < 1)
            page = 1;

        if (pageSize < 1 || pageSize > 100)
            pageSize = 50;

        var result = await _documentRepository.List(page, pageSize);

        if (result.IsFailure)
            return StatusCode(500, new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Список марок остатка ГИС МТ с поиском, отборами по организации, группе, сроку годности и признаку продажи и пагинацией.
    /// </summary>
    [HttpGet("marks")]
    public async Task<IActionResult> GetMarks(
        [FromQuery] string? search = null,
        [FromQuery] string? productGroup = null,
        [FromQuery] string? organisationInn = null,
        [FromQuery] bool expiredOnly = false,
        [FromQuery] bool? sold = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var settings = await _parametersService.CurrentAsync();

        if (!settings.Database.Enable)
            return BadRequest("База данных отключена");

        if (!_appState.CouchDbOnline())
            return BadRequest("База данных недоступна в данный момент");

        if (page < 1)
            page = 1;

        if (pageSize < 1 || pageSize > 100)
            pageSize = 50;

        var result = await _markRepository.Search(
            search ?? string.Empty,
            page,
            pageSize,
            productGroup,
            expiredOnly,
            sold,
            organisationInn);

        if (result.IsFailure)
            return StatusCode(500, result.Error);

        return Ok(result.Value);
    }
}
