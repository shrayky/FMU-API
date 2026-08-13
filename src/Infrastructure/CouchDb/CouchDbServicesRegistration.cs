using CouchDb.Repositories;
using CouchDb.Services;
using CouchDb.Workers;
using CouchDb.Workers.DatabaseMigrationWorkers;
using CouchDB.Driver;
using CouchDB.Driver.Options;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

namespace CouchDb;

public static class CouchDbServicesRegistration
{
    public static void AddService(IServiceCollection services)
    {
        using var scope = services.BuildServiceProvider().CreateScope();
        var configService = scope.ServiceProvider.GetRequiredService<IParametersService>();
        var settings = configService.Current();

        var endpoint = settings.Database.ConfigurationIsEnabled
            ? settings.Database.NetAddress
            : "http://localhost:59841";

        var userName = settings.Database.ConfigurationIsEnabled
            ? settings.Database.UserName
            : "no";

        var password = settings.Database.ConfigurationIsEnabled
            ? settings.Database.Password
            : "no";

        // HttpClient с таймаутом и отключенной проверкой сертификата — вместо ConfigureFlurlClient из 3.x
        var httpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        })
        {
            Timeout = TimeSpan.FromSeconds(settings.Database.QueryTimeoutSeconds)
        };

        var clientOptions = new CouchClientOptions
        {
            HttpClient = httpClient,
            // В 4.x по умолчанию true; сохраняем прежнее поведение, чтобы mango без идеального индекса не падал
            ThrowOnQueryWarning = false,
            JsonSerializerOptions = JsonSerializerOptions.Web
        };

        services.AddSingleton(_ => new CouchClient(
            endpoint,
            new BasicCredentials(userName, password),
            clientOptions));

        services.AddSingleton(provider =>
            new CouchDbContext(provider.GetRequiredService<CouchClient>()));

        services.AddAutoRegisteredServices([Assembly.GetExecutingAssembly()]);

        services.AddScoped<IMarkInformationRepository, MarkInformationRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<ICheckStatisticRepository, MarkCheckingStatisticRepository>();
        services.AddScoped<IBeerOnTapsRepository, BeerOnTapRepository>();
        services.AddScoped<IGisMtDocumentRepository, GisMtDocumentRepository>();
        services.AddScoped<IGisMtMarkRepository, GisMtMarkRepository>();
        services.AddSingleton<DataBaseMaintenanceService>();

        services.AddHttpClient("CouchDbState", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(settings.Database.QueryTimeoutSeconds);
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
        services.AddHostedService<ClearingStorageOfStatisticsWorker>();
    }
}
