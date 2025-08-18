using CouchDb.Repositories;
using CouchDb.Workers;
using CouchDb.Workers.DatabaseMigrationWorkers;
using CouchDB.Driver.DependencyInjection;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;

namespace CouchDb
{
    public class CouchDbServicesRegistration
    {
        public static void AddService(IServiceCollection services)
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            var configService = scope.ServiceProvider.GetRequiredService<IParametersService>();
            var settings = configService.Current();

            services.AddCouchContext<CouchDbContext>(options =>
            {
                if (!settings.Database.ConfigurationIsEnabled)
                {
                    options.UseEndpoint("http://localhost:59841");
                    options.UseBasicAuthentication("no", "no");
                }
                else
                {
                    options.UseEndpoint(settings.Database.NetAddress);
                    options.UseBasicAuthentication(settings.Database.UserName, settings.Database.Password);
                    //options.EnsureDatabaseExists();
                }
            });

            DatabaseNames.Initialize(settings.Database);

            // устарело, удалить:
            //services.AddScoped<FrontolDocumentHandler>();
            
            services.AddScoped<IMarkInformationRepository, MarkInformationRepository>();
            services.AddScoped<IDocumentRepository, DocumentRepository>();

            services.AddHttpClient("CouchDbState", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(5);
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));

            services.AddHostedService<CouchDbStatusWorker>();
            services.AddHostedService<CouchDbMigrationTo102Worker>();
        }
    }
}
