using FmuApiApplication.Statistics;
using FmuApiDomain.MarkInformation.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Statistics;

[Route("api/statistics")]
[ApiController]
[ApiExplorerSettings(GroupName = "Statistics")]
public class StatisticsController(IMarkStatisticsService statisticsService) : ControllerBase
{
    [HttpPost("clear")]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken)
    {
        var result = await statisticsService.ClearAll(cancellationToken);
        if (result.IsFailure)
            return MapError(result.Error);

        return Ok();
    }

    private IActionResult MapError(string error) => error switch
    {
        MarkStatisticsService.DatabaseDisabled or MarkStatisticsService.DatabaseUnavailable => BadRequest(new { error }),
        _ => StatusCode(500, new { error })
    };
}
