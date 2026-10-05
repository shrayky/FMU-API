using FmuApiDomain.BeerTaps.Interfaces;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Frontol.Interfaces;
using FrontolDb.Services;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Configuration;

[Route("api/configuration/[controller]")]
[ApiController]
[ApiExplorerSettings(GroupName = "App configuration")]
public class FrontolConnectionController : Controller
{
    private readonly FrontolAdminIniReader _iniReader;
    private readonly IBeerOnTapManager _beerOnTapManager;
    private readonly IFrontolConnectionProbe _connectionProbe;

    public FrontolConnectionController(
        FrontolAdminIniReader iniReader,
        IBeerOnTapManager beerOnTapManager,
        IFrontolConnectionProbe connectionProbe)
    {
        _iniReader = iniReader;
        _beerOnTapManager = beerOnTapManager;
        _connectionProbe = connectionProbe;
    }

    [HttpGet("import-from-admin")]
    public IActionResult ImportFromAdmin()
    {
        var (success, error, connections) = _iniReader.Read();

        if (!success)
            return NotFound(new { message = error });

        return Ok(connections);
    }

    [HttpPost("load-beer-taps")]
    public async Task<IActionResult> LoadBeerTaps([FromQuery] int connectionId)
    {
        var result = await _beerOnTapManager.LoadFromFrontol(connectionId);

        if (result.IsFailure)
            return BadRequest(new { message = result.Error });

        return Ok(new { loaded = result.Value });
    }

    // проверяет связь по переданным параметрам, настройки приложения не меняет
    [HttpPost("test")]
    public async Task<IActionResult> TestConnection(
        [FromBody] FrontolConnectionTestRequest request,
        CancellationToken cancellationToken)
    {
        var connection = new FrontolConnectionSettings
        {
            Path = request.Path?.Trim() ?? string.Empty,
            UserName = request.UserName?.Trim() ?? string.Empty,
            Password = request.Password ?? string.Empty
        };

        if (!connection.ConnectionEnable())
            return BadRequest(new { message = "Укажите путь, пользователя и пароль" });

        var result = await _connectionProbe.Probe(connection, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Связь установлена" });
    }
}

/// <summary>
/// Параметры проверки связи с базой Frontol из окна настроек.
/// </summary>
public class FrontolConnectionTestRequest
{
    public string? Path { get; set; }

    public string? UserName { get; set; }

    public string? Password { get; set; }
}
