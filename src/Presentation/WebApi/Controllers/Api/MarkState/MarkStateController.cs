using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.MarkInformation.Models;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.MarkState
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "Mark state API")]
    public class MarkStateController : ControllerBase
    {
        private readonly ILogger<MarkStateController> _logger;
        private IMarkStateManager _markStateService;

        public MarkStateController(ILogger<MarkStateController> logger, IMarkStateManager markStateService)
        {
            _logger = logger;
            _markStateService = markStateService;
        }

        [HttpGet]
        public async Task<IActionResult> StateMark(string mark)
        {
            var info = await _markStateService.Information(mark);

            return Ok(info.State);
        }

        [HttpPost]
        public async Task<IActionResult> SaleMark(CheckWithMarks saleMark)
        {
            foreach (var sgtin in saleMark.Marks)
            {
                await _markStateService.ChangeState(sgtin, saleMark.CheckData.IsSale ? "sold" : "stock", saleMark.CheckData);
            }

            return Ok();
        }

    }
}
