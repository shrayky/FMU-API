using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.State.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.TrueSign;

[Route("api/ts/gismt")]
[ApiController]
[ApiExplorerSettings(GroupName = "True API")]
public class GisMtDocumentsController : ControllerBase
{
    private readonly IGisMtDocumentsSyncService _documentsSyncService;
    private readonly IGisMtProductGroupsService _productGroupsService;
    private readonly IGisMtStockLoadService _stockLoadService;
    private readonly IGisMtMarkRepository _markRepository;
    private readonly IParametersService _parametersService;
    private readonly IApplicationState _appState;

    public GisMtDocumentsController(
        IGisMtDocumentsSyncService documentsSyncService,
        IGisMtProductGroupsService productGroupsService,
        IGisMtStockLoadService stockLoadService,
        IGisMtMarkRepository markRepository,
        IParametersService parametersService,
        IApplicationState applicationState)
    {
        _documentsSyncService = documentsSyncService;
        _productGroupsService = productGroupsService;
        _stockLoadService = stockLoadService;
        _markRepository = markRepository;
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
    /// Список марок остатка ГИС МТ с поиском, отбором по группе и пагинацией.
    /// </summary>
    [HttpGet("marks")]
    public async Task<IActionResult> GetMarks(
        [FromQuery] string? search = null,
        [FromQuery] string? productGroup = null,
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

        var result = await _markRepository.Search(search ?? string.Empty, page, pageSize, productGroup);

        if (result.IsFailure)
            return StatusCode(500, result.Error);

        return Ok(result.Value);
    }
}
