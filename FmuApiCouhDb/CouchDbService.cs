using CouchDb.Handlers;
using CouchDB.Driver.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace CouchDb
{
    public class CouchDbService
    {
        public static void AddService(IServiceCollection services)
        {
            services.AddCouchContext<CouchDbContext>(options => { });

            services.AddScoped<FrontolDocumentHandler>();
            services.AddScoped<MarkInformationHandler>();

        }
    }
}
