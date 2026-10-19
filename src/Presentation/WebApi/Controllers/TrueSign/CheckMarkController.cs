using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Mark.Interfaces;
using FmuApiDomain.TrueApi.MarkData.Check;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.TrueSign;

[Route("api/ts/[controller]")]
[ApiController]
[ApiExplorerSettings(GroupName = "True API")]
public class CheckMarkController : ControllerBase
{
    private readonly IOnLineMarkCheckService _onlineMarkCheck;
    private readonly IParametersService _parametersService;

    public CheckMarkController(IOnLineMarkCheckService checkMarks, IParametersService parametersService)
    {
        _onlineMarkCheck = checkMarks;
        _parametersService = parametersService;
    }

    [HttpPost]
    public async Task<IActionResult> CheckMark(List<string> marks)
    {
        CheckMarksRequestData checkMarksRequestData = new(marks);

        var trueMarkCheckResult = await _onlineMarkCheck.RequestMarkState(checkMarksRequestData);

        if (trueMarkCheckResult.IsFailure)
            return NotFound(trueMarkCheckResult.Error);

        return Ok(trueMarkCheckResult.Value);
    }

    [HttpPost("inn")]
    public async Task<IActionResult> CheckMarkByInn([FromQuery] string inn, [FromBody] List<string> marks)
    {
        var settings = await _parametersService.CurrentAsync();
        var group = settings.OrganisationConfig.PrintGroupByInn(inn);

        if (group == null)
            return NotFound($"Организация с ИНН {inn} не найдена");

        var trueMarkCheckResult = await _onlineMarkCheck.RequestMarkState(new CheckMarksRequestData(marks), group);

        if (trueMarkCheckResult.IsFailure)
            return NotFound(trueMarkCheckResult.Error);

        return Ok(trueMarkCheckResult.Value);
    }
}
