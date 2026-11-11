using FmuApiDomain.Attributes;
using FmuApiDomain.GisMt.Interfaces;
using System.Reflection;
using Xunit;

namespace FmuApiApplication.Tests;

public class GisMtJournalRegistrationTests
{
    private static HashSet<Type> AutoRegisteredInterfaces(Assembly assembly) => assembly
        .GetTypes()
        .Where(type => type.IsClass && !type.IsAbstract)
        .Where(type => type.GetCustomAttribute<AutoRegisterServiceAttribute>() != null)
        .SelectMany(type => type.GetInterfaces())
        .ToHashSet();

    [Fact]
    public void Сервисы_журнала_найдены_сканированием_сборки_приложения()
    {
        var registered = AutoRegisteredInterfaces(typeof(FmuApiApplication.GisMt.Journal.GisMtJournalService).Assembly);

        Assert.Contains(typeof(IGisMtJournalService), registered);
        Assert.Contains(typeof(IGisMtReceiptService), registered);
        Assert.Contains(typeof(IGisMtConnectTapService), registered);
        Assert.Contains(typeof(IGisMtModsService), registered);
    }

    [Fact]
    public void Репозиторий_журнала_есть_в_CouchDB()
    {
        Assert.True(typeof(IGisMtJournalRepository)
            .IsAssignableFrom(typeof(CouchDb.Repositories.GisMtJournalRepository)));
    }

    [Fact]
    public void Репозиторий_журнала_есть_в_SQLite()
    {
        Assert.True(typeof(IGisMtJournalRepository)
            .IsAssignableFrom(typeof(Sqlite.Repositories.GisMtJournalRepository)));
    }
}
