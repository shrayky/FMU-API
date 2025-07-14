using FmuApiApplication.Services.TrueSign;
using FmuApiDomain.TrueApi.MarkData.Check;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.TrueSign
{
    [Route("api/ts/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "True API")]
    public class CheckMarkController : ControllerBase
    {
        private readonly MarksCheckService _checkMarks;

        public CheckMarkController(MarksCheckService checkMarks)
        {
            _checkMarks = checkMarks;
        }

        [HttpPost]
        public async Task<IActionResult> CheckMark(List<string> marks)
        {
            CheckMarksRequestData checkMarksRequestData = new(marks);

            var trueMarkCheckResult = await _checkMarks.RequestMarkState(checkMarksRequestData);

            if (trueMarkCheckResult.IsFailure)
            {
                return NotFound(trueMarkCheckResult.Error);
            }

            return Ok(trueMarkCheckResult.Value);
        }
    }
}
