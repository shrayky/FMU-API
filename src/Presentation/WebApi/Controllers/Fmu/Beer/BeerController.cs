using FmuApiDomain.Fmu.BeerTaps.Interfaces;
using FmuApiDomain.Fmu.BeerTaps.Models;
using FmuApiDomain.Fmu.Document;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Fmu.Beer;

[Route("api/fmu/[controller]")]
[Route("api/fmu/{inn}/[controller]")]
[Route("[controller]")]
[Route("{inn}/[controller]")]
[ApiController]
[ApiExplorerSettings(GroupName = "Frontol mark unit API")]
public class BeerController : ControllerBase
{
    private readonly IBeerOnTapManager _beerOnTapManager;

    public BeerController(IBeerOnTapManager beerOnTapManager)
    {
        _beerOnTapManager = beerOnTapManager;
    }

    [HttpPost("/beer/connect_keg")]
    [HttpPost("/api/fmu/beer/connect_keg")]
    public async Task<IActionResult> ConnectKeg(TapBeerOperation document)
    {
        var result = await _beerOnTapManager.TapOperation(document);

        FmuAnswer answer = new();
        
        return result.IsSuccess ? Ok(answer) : BadRequest(result.Error);
    }

    [HttpGet("/api/beer-taps")]
    public async Task<IActionResult> List() => Ok(await _beerOnTapManager.List());
}
