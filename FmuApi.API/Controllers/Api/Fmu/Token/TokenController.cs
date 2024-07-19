using FmuApiDomain.Models.Fmu.Token;
using FmuApiSettings;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FmuApiAPI.Controllers.Api.Fmu.Token
{
    [Route("api/fmu/[controller]")]
    [Route("[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "Frontol mark unit API")]
    public class TokenController : ControllerBase
    {
        private readonly int tokenLifeMinutes = 60;

        [HttpGet]
        public IActionResult TokenGet()
        {
            DateTime expired = DateTime.Now.AddMinutes(tokenLifeMinutes).ToUniversalTime();

            AuthorizationAnswer authorizationAnswer = new()
            {
                Id = "Pos",
                Name = "",
                Expired = (int)expired.Subtract(DateTime.UnixEpoch).TotalSeconds,
                Signature = "fmu-api-sign"
            };

            string configJson = JsonSerializer.Serialize(authorizationAnswer);
            byte[] encodedByte = System.Text.Encoding.ASCII.GetBytes(configJson);
            string signature = Convert.ToBase64String(encodedByte);

            Constants.FmuToken.Signature = signature;
            Constants.FmuToken.Expired = expired;

            return Ok(configJson);
        }
    }
}
