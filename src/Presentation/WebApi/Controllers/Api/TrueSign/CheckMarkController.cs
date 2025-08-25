using FmuApiDomain.TrueApi.Interfaces;
using FmuApiDomain.TrueApi.MarkData.Check;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.TrueSign
{
    [Route("api/ts/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "True API")]
    public class CheckMarkController : ControllerBase
    {
        private readonly IOnLineMarkCheckService _onlineMarkCheck;

        public CheckMarkController(IOnLineMarkCheckService checkMarks)
        {
            _onlineMarkCheck = checkMarks;
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
    }
}
