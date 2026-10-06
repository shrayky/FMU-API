using FmuApiDomain.BeerTaps.Interfaces;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Database.Interfaces;
using FmuApiDomain.Documents.Interfaces;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.Mark.Interfaces;
using FmuApiDomain.ProductGroups.Interfaces;
using FmuApiDomain.Statistics.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sqlite.Documents;
using Sqlite.Repositories;
using Sqlite.Services;
using Sqlite.Workers;

namespace Sqlite;

/// <summary>
/// Регистрация хранилища SQLite и сервиса переноса данных из CouchDB.
/// </summary>
public static class SqliteService
{
    /// <summary>
    /// Регистрирует файл SQLite: контекст, репозитории и воркер статуса.
    /// </summary>
    public static void AddService(IServiceCollection services)
    {
        using var scope = services.BuildServiceProvider().CreateScope();
        var configService = scope.ServiceProvider.GetRequiredService<IParametersService>();
        var settings = configService.Current();
        var connectionString = SqliteConnectionBuilder.Build(settings.Database);

        services.AddDbContext<SqliteDbContext>(options => options.UseSqlite(connectionString));

        AddRepository<IMarkInformationRepository, MarkInformationRepository>(services);
        AddRepository<IDocumentRepository, DocumentRepository>(services);
        AddRepository<ICheckStatisticRepository, MarkCheckingStatisticRepository>(services);
        AddRepository<IBeerOnTapRepository, BeerOnTapRepository>(services);
        AddRepository<IGisMtDocumentRepository, GisMtDocumentRepository>(services);
        AddRepository<IGisMtMarkRepository, GisMtMarkRepository>(services);
        AddRepository<IGtinCatalogRepository, GtinCatalogRepository>(services);

        services.AddHostedService<SqliteStatusWorker>();

        EnsureDatabaseCreated(services, settings.Database);
    }

    /// <summary>
    /// Регистрирует сервис переноса: страница «Сервис» работает при любом провайдере.
    /// </summary>
    public static void AddImportService(IServiceCollection services)
    {
        services.AddSingleton<ICouchDbToSqliteImport, CouchDbToSqliteImport>();
    }

    /// <summary>
    /// Читает сохранённый провайдер базы данных, чтобы выбрать регистрацию хранилища.
    /// </summary>
    public static DatabaseProvider ResolveProvider(IServiceCollection services)
    {
        using var scope = services.BuildServiceProvider().CreateScope();

        return scope.ServiceProvider.GetRequiredService<IParametersService>().Current().Database.Provider;
    }

    private static void AddRepository<TInterface, TImplementation>(IServiceCollection services)
        where TImplementation : class, TInterface, ISqliteImportTarget
        where TInterface : class
    {
        services.AddScoped<TImplementation>();
        services.AddScoped<TInterface>(provider => provider.GetRequiredService<TImplementation>());
        services.AddScoped<ISqliteImportTarget>(provider => provider.GetRequiredService<TImplementation>());
    }

    private static void EnsureDatabaseCreated(IServiceCollection services, CouchDbConnection database)
    {
        using var startupScope = services.BuildServiceProvider().CreateScope();
        var logger = startupScope.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger(nameof(SqliteService));

        try
        {
            CreateDatabaseFolder(database);
            startupScope.ServiceProvider.GetRequiredService<SqliteDbContext>().Database.EnsureCreated();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Не удалось создать файл базы данных SQLite {Path}", database.ResolvedSqlitePath);
        }
    }

    private static void CreateDatabaseFolder(CouchDbConnection database)
    {
        var folder = Path.GetDirectoryName(database.ResolvedSqlitePath);

        if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            Directory.CreateDirectory(folder);
    }
}
