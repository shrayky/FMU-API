using FmuApiDomain.BeerTaps.Interfaces;
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

    public FrontolConnectionController(FrontolAdminIniReader iniReader, IBeerOnTapManager beerOnTapManager)
    {
        _iniReader = iniReader;
        _beerOnTapManager = beerOnTapManager;
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
}
