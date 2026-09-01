using FmuApiDomain.Connectivity.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Connectivity;

[Route("api/crpt-esp-check")]
[ApiController]
[ApiExplorerSettings(GroupName = "App configuration")]
public class CrptEspCheckController(ICrptEspConnectivityService connectivityService) : ControllerBase
{
    [HttpGet]
    public IActionResult Hosts()
    {
        return Ok(connectivityService.ListHosts());
    }

    [HttpPost]
    public async Task<IActionResult> Check(CancellationToken cancellationToken)
    {
        try
        {
            var result = await connectivityService.CheckAll(cancellationToken);
            return Ok(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
