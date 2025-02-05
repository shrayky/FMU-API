using FmuApiApplication.Services.TrueSign;
using FmuApiDomain.State.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.TrueSign
{
    [Route("api/ts/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "True API")]
    public class ProductInfoController : ControllerBase
    {
        private readonly ProductInfo _productInfo;
        private readonly IApplicationState _applicationState;

        public ProductInfoController(ProductInfo productInfo, IApplicationState applicationState)
        {
            _productInfo = productInfo;
            _applicationState = applicationState;
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync(List<string> gtins)
        {
            if (!_applicationState.IsOnline())
                return BadRequest("Нет доступа к интернету.");

            if (_applicationState.TrueApiToken().Token == string.Empty)
                return BadRequest("Нет токена true api полученного с помощью УКЭП");

            if (_applicationState.TrueApiToken().Expired < DateTime.Now)
                return BadRequest("Токен УКЭП истек, необходимо получить новый.");

            var info = await _productInfo.Load(gtins);

            if (info.IsSuccess)
                return Ok(info.Value);

            return BadRequest(info.Error);

        }

    }
}
