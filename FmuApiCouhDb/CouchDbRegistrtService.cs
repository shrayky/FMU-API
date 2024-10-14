using CouchDB.Driver.DependencyInjection;
using FmuApiCouhDb.CrudServices;
using Microsoft.Extensions.DependencyInjection;

namespace FmuApiCouhDb
{
    public class CouchDbRegistrtService
    {
        public static void AddService(IServiceCollection services)
        {
            services.AddCouchContext<CouchDbContext>(options => { });

            services.AddScoped<FrontolDocumentHandler>();
            services.AddScoped<MarkInformationHandler>();
        }
    }
}
