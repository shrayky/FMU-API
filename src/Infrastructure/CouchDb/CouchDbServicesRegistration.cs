using CouchDb.Repositories;
using CouchDb.Workers;
using CouchDb.Workers.DatabaseMigrationWorkers;
using CouchDB.Driver.DependencyInjection;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Reflection;
using CouchDb.Services;

namespace CouchDb
{
    public static class CouchDbServicesRegistration
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
                }
                
                options.ConfigureFlurlClient(clientFlurlHttpSettings =>
                    clientFlurlHttpSettings.Timeout = TimeSpan.FromSeconds(settings.Database.QueryTimeoutSeconds));
            });

            services.AddAutoRegisteredServices([Assembly.GetExecutingAssembly()]);

            // TODO удалить в 11 релизе
            DatabaseNames.Initialize(settings.Database);

            services.AddScoped<IMarkInformationRepository, MarkInformationRepository>();
            services.AddScoped<IDocumentRepository, DocumentRepository>();
            services.AddScoped<ICheckStatisticRepository, MarkCheckingStatisticRepository>();
            services.AddSingleton<DataBaseMaintenanceService>();

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
            services.AddHostedService<DatabaseCompactWorker>();
            services.AddHostedService<CouchDbMigrationTo102Worker>();
        }
    }
}
