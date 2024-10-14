using FmuApiApplication.Services.Frontol;
using Microsoft.Extensions.DependencyInjection;

namespace FmuFrontolDb
{
    public class FrontolDbRegisterService
    {
        public static void AddService(IServiceCollection services) 
        {
            services.AddDbContext<FrontolDbContext>(options => { });
            
            services.AddScoped<FrontolSprtDataHandler>();
            
        }
    }
}
