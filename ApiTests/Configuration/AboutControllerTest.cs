using FmuApiAPI.Controllers.Api.Configuration;

namespace FmuApiAoiTests.Configuration
{
    public class AboutControllerTest
    {
        [Fact]
        public async Task Get_About()
        {
            var controller = new AboutController();

            var result = await controller.Get();
        }
    }
}
