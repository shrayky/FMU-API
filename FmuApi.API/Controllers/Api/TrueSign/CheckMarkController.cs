using FmuApiApplication.Services.TrueSign;
using FmuApiDomain.Models.TrueSignApi.MarkData.Check;
using Microsoft.AspNetCore.Mvc;

namespace FmuApiAPI.Controllers.Api.TrueSign
{
    [Route("api/ts/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "True API")]
    public class CheckMarkController : ControllerBase
    {
        private readonly CheckMarks _checkMarks;

        public CheckMarkController(CheckMarks checkMarks)
        {
            _checkMarks = checkMarks;
        }

        [HttpPost]
        public async Task<IActionResult> CheckMarkGet(List<string> marks)
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
