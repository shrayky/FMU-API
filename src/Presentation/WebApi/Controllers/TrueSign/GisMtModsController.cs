using FmuApiDomain.GisMt.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.TrueSign;

[Route("api/ts/gismt/mods")]
[ApiController]
[ApiExplorerSettings(GroupName = "True API")]
public class GisMtModsController : ControllerBase
{
    private readonly IGisMtModsService _modsService;

    public GisMtModsController(IGisMtModsService modsService)
    {
        _modsService = modsService;
    }

    /// <summary>
    /// Возвращает места осуществления деятельности организации для выбора КПП и ФИАС.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Places([FromQuery] string? inn, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(inn))
            return BadRequest(new { error = "Укажите ИНН организации" });

        var result = await _modsService.Places(inn, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }
}
