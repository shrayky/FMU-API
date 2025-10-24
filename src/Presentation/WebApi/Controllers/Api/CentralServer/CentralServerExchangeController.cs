using FmuApiDomain.CentralServiceExchange.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.CentralServer;

[Route("api/centralServer/[controller]")]
[ApiController]
[ApiExplorerSettings(GroupName = "Central server exchange")]
public class CentralServerExchangeController : ControllerBase
{
    private readonly ILogger<CentralServerExchangeController> _logger;
    private readonly ICentralServerExchangeActions  _exchangeActions;

    public CentralServerExchangeController(ILogger<CentralServerExchangeController> logger, ICentralServerExchangeActions exchangeActions)
    {
        _logger = logger;
        _exchangeActions = exchangeActions;
    }
    
    [HttpGet]
    public async Task<IActionResult> StartExchange()
    {
        _logger.LogInformation("Starting exchange...");
        
        var exchangeResult = await _exchangeActions.StartExchange();
        
        return Ok(exchangeResult);
    }
}