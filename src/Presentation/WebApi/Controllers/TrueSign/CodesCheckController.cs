using FmuApiDomain.TrueApi.Interfaces;
using FmuApiDomain.TrueApi.MarkData.Check;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.TrueSign;

/// <summary>
/// Эндпоинт True API codes/check: клиент работает по X-API-KEY, проверки идут через все источники FMU-API.
/// </summary>
[Route("api/v4/true-api/codes/check")]
[ApiController]
[ApiExplorerSettings(GroupName = "True API")]
public class CodesCheckController : ControllerBase
{
    private readonly ITrueApiCodesCheckService _codesCheckService;

    public CodesCheckController(ITrueApiCodesCheckService codesCheckService)
    {
        _codesCheckService = codesCheckService;
    }

    [HttpPost]
    public async Task<IActionResult> Check([FromBody] CheckMarksRequestData request)
    {
        if (request is null || request.Codes.Count == 0)
            return BadRequest("Список codes обязателен");

        var xApiKey = Request.Headers["X-API-KEY"].FirstOrDefault();
        var result = await _codesCheckService.Check(request, xApiKey);

        if (result.IsFailure)
            return NotFound(result.Error);

        return Ok(result.Value);
    }
}
