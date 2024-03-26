using FmuApiApplication;
using FmuApiApplication.Services.TrueSign;
using FmuApiSettings;
using Microsoft.AspNetCore.Mvc;

namespace FmuApiAPI.Controllers.Api.TrueSign
{
    [Route("api/ts/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "True API")]
    public class ProductInfoController : ControllerBase
    {
        private readonly ProductInfo _productInfo;

        public ProductInfoController(ProductInfo productInfo)
        {
            _productInfo = productInfo;
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync(List<string> gtins) 
        {
            if (!Constants.Online)
                return BadRequest("Нет доступа к интеренету.");

            if (Constants.Parametrs.SignData.Signature == string.Empty)
                return BadRequest("Нет токена true api полученного с помощью УКЭП");

            if (Constants.Parametrs.SignData.Expired < DateTime.Now)
                return BadRequest("Токен УКЭП истек, необъодимо получить новый.");

            var info = await _productInfo.Load(gtins);

            if (info.IsSuccess)
                return Ok(info.Value);

            return BadRequest(info.Error);

        }

    }
}
