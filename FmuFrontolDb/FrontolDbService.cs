using FrontolDb.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace FrontolDb
{
    public class FrontolDbService
    {
        public static void AddService(IServiceCollection services)
        {
            services.AddDbContext<FrontolDbContext>(options => { });

            services.AddScoped<FrontolSprtDataHandler>();

        }
    }
}
