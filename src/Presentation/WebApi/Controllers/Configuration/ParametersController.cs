using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.State.Interfaces;
using FmuApiDomain.Webix;
using Microsoft.AspNetCore.Mvc;
using Shared.Json;
using System.Text;
using System.Text.Json;

namespace WebApi.Controllers.Configuration;

[Route("api/configuration/[controller]")]
[ApiController]
[ApiExplorerSettings(GroupName = "App configuration")]
public class ParametersController : Controller
{
    private readonly IParametersService _parametersService;
    private readonly IApplicationState _appState;

    private readonly Parameters _configuration;

    public ParametersController(IParametersService parametersService, IApplicationState applicationState)
    {
        _parametersService = parametersService;
        _appState = applicationState;

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

        var answer = new
        {
            isSuccess = true,
            needToRestart = _appState.NeedRestartService(),
        };

        return Ok(answer);
    }

    [HttpGet("exchange")]
    public IActionResult Export()
    {
        var json = SettingsExchange.Export(_parametersService.Current());
        return File(Encoding.UTF8.GetBytes(json), "application/json", "config.json");
    }

    [HttpPost("exchange")]
    public async Task<IActionResult> Import()
    {
        string json;

        try
        {
            using var body = new StreamReader(Request.Body);
            json = await body.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            return BadRequest($"Не удалось прочитать файл настроек. {ex.Message}");
        }

        var settings = _parametersService.Current();
        if (!SettingsExchange.TryApply(settings, json, out var changed, out var error))
            return BadRequest($"Не удалось импортировать настройки. {error}");

        if (changed)
            await _parametersService.UpdateAsync(settings);

        return Ok(new
        {
            isSuccess = true,
            needToRestart = _appState.NeedRestartService(),
        });
    }
}
