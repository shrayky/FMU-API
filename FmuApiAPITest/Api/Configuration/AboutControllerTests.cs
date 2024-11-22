using WebApi.Controllers.Api.Configuration;
using FmuApiSettings;
using Microsoft.AspNetCore.Mvc;

namespace FmuApiAPITest.Api.Configuration
{
    public class AboutControllerTests
    {
        [Fact]
        public void Get_AboutInformation()
        {
            var controller = new AboutController();
            var result = controller.AboutGet();

            Assert.NotNull(result);

            var okResult = Assert.IsType<OkObjectResult>(result);

            Assert.Equal(okResult.Value, $"{Constants.Parametrs.AppName} version {Constants.Parametrs.AppVersion} assembly {Constants.Parametrs.Assembly}");
        }
    }
}
