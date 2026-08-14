using FmuApiApplication.Mark.Services;
using FmuApiDomain.Mark.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.MarkState;

[Route("api/marks")]
[ApiController]
[ApiExplorerSettings(GroupName = "Marks")]
public class MarksController(IMarksListService marksListService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMarks([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var result = await marksListService.List(search ?? string.Empty, page, pageSize);
        if (result.IsFailure)
            return MapError(result.Error);

        return Ok(result.Value);
    }

    [HttpGet("checks")]
    public async Task<IActionResult> CheckInformation([FromQuery] string id)
    {
        var result = await marksListService.CheckInformation(id);
        if (result.IsFailure)
            return MapError(result.Error);

        return Ok(result.Value);
    }

    private IActionResult MapError(string error) => error switch
    {
        MarksListService.DatabaseDisabled or MarksListService.DatabaseUnavailable => BadRequest(error),
        MarksListService.CheckNotFound => NotFound(error),
        _ => StatusCode(500, error)
    };
}
