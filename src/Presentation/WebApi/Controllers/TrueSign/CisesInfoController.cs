using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Models;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.TrueSign;

[Route("api/ts/cises/info")]
[ApiController]
[ApiExplorerSettings(GroupName = "True API")]
public class CisesInfoController : ControllerBase
{
    private readonly IMarkCheckTrueApiService _markCheckTrueApiService;

    public CisesInfoController(IMarkCheckTrueApiService markCheckTrueApiService)
    {
        _markCheckTrueApiService = markCheckTrueApiService;
    }

    [HttpPost]
    public async Task<IActionResult> PostAsync(
        [FromBody] MarkCheckTrueApiRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Inn))
            return BadRequest(new { error = "Параметр inn обязателен" });

        if (request.Cises is null || request.Cises.Count == 0)
            return BadRequest(new { error = "Список cises обязателен" });

        var cises = request.Cises
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToList();

        if (cises.Count == 0)
            return BadRequest(new { error = "Список cises обязателен" });

        var result = await _markCheckTrueApiService.CisesInfo(
            request.Inn.Trim(),
            cises,
            cancellationToken);

        return Ok(result);
    }
}
