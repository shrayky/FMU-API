using FmuApiDomain.Configuration;
using FmuApiDomain.Webix;
using FmuApiSettings;
using Interfaces;
using JsonSerialShared.Json;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace WebApi.Controllers.Api.Configuration
{
    [Route("api/configuration/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "App configuration")]
    public class ParametersController : Controller
    {
        private readonly IParametersService _parametersService;
        private readonly Parameters _configuration;

        public ParametersController(IParametersService parametersService)
        {
            _parametersService = parametersService;
            _configuration = _parametersService.Current();
        }

        [HttpGet]
        public IActionResult ParametersGet()
        {
            WebixDataPacket packet = new()
            {
                Content = _configuration
            };

            return Ok(packet);
        }

        [HttpPost]
        async public Task<IActionResult> ParametersPostAsync()
        {
            StreamReader? body = new(Request.Body);

            if (body is null)
                return BadRequest("Пустое тело запроса");

            Parameters? loadPrm;

            try
            {
                loadPrm = await JsonSerializer.DeserializeAsync<Parameters>(body.BaseStream, JsonSerializeOptionsProvider.Default());
            }
            catch (Exception ex)
            {
                return BadRequest($"Не удалось преобразовать входящий пакет данных! {ex.Message}");
            }

            if (loadPrm == null)
                return BadRequest();

            await _parametersService.UpdateAsync(loadPrm);

            return Ok();
        }
    }
}
