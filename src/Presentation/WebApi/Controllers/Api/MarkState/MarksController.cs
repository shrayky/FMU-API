using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Repositories;
using FmuApiDomain.State.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.MarkState
{
    [Route("api/marks")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "Marks")]
    public class MarksController : ControllerBase
    {
        private readonly IMarkInformationRepository _markRepository;
        private readonly IParametersService _parametersService;
        private readonly IApplicationState _appState;
        
        public MarksController(IMarkInformationRepository markRepository, IParametersService parametersService, IApplicationState applicationState)
        {
            _markRepository = markRepository;
            _parametersService = parametersService;
            _appState = applicationState;
        }

        [HttpGet]
        public async Task<IActionResult> GetMarks([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var settings = await _parametersService.CurrentAsync();

            if (!settings.Database.Enable)
                return BadRequest("База данных отключена");

            if (!_appState.CouchDbOnline())
                return BadRequest("База данных недоступна в данный момент");

            if (page < 1) 
                page = 1;

            if (pageSize < 1 || pageSize > 100) 
                pageSize = 50;

            try
            {
                var result = await _markRepository.SearchMarkData(
                    search ?? string.Empty,
                    page,
                    pageSize);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при получении списка марок: {ex.Message}");
            }
        }

    }
}
