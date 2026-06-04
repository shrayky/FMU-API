using FrontolDb.Services;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.Configuration;

[Route("api/configuration/[controller]")]
[ApiController]
[ApiExplorerSettings(GroupName = "App configuration")]
public class FrontolConnectionController : Controller
{
    private readonly FrontolAdminIniReader _iniReader;

    public FrontolConnectionController(FrontolAdminIniReader iniReader)
    {
        _iniReader = iniReader;
    }

    [HttpGet("import-from-admin")]
    public IActionResult ImportFromAdmin()
    {
        var (success, error, connections) = _iniReader.Read();

        if (!success)
            return NotFound(new { message = error });

        return Ok(connections);
    }
}
