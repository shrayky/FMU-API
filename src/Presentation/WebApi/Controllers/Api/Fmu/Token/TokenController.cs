using FmuApiDomain.Authentication.Models;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Fmu.Token;
using FmuApiDomain.State.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace WebApi.Controllers.Api.Fmu.Token
{
    [Route("api/fmu/[controller]")]
    [Route("[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "Frontol mark unit API")]
    public class TokenController : ControllerBase
    {
        private readonly IApplicationState _applicationState;
        public TokenController(IApplicationState applicationState) 
        {
            _applicationState = applicationState;
        }

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

            TokenData token = new(signature, expired);

            _applicationState.UpdateFmuToken(token);

            return Ok(configJson);
        }
    }
}
