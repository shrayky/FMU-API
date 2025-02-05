using CouchDb.Handlers;
using CouchDB.Driver.DependencyInjection;
using FmuApiDomain.Configuration.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CouchDb
{
    public class CouchDbService
    {
        public static void AddService(IServiceCollection services)
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            var configService = scope.ServiceProvider.GetRequiredService<IParametersService>();
            var settings = configService.Current();

            services.AddCouchContext<CouchDbContext>(options =>
            {
                if (string.IsNullOrEmpty(settings.Database.NetAdres))
                {
                    options.UseEndpoint("http://localhost:5984");
                    options.UseBasicAuthentication("no", "no");
                }
                else
                {
                    options.UseEndpoint(settings.Database.NetAdres);
                    options.UseBasicAuthentication(settings.Database.UserName, settings.Database.Password);
                    options.EnsureDatabaseExists();
                }
            });

            services.AddScoped<FrontolDocumentHandler>();
            services.AddScoped<MarkInformationHandler>();
        }
    }
}
