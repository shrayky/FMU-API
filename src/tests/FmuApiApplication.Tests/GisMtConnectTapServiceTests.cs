using FmuApiDomain.BeerTaps.Models;
using FmuApiDomain.Documents;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Journal;
using Xunit;
using BeerTapPosition = FmuApiDomain.BeerTaps.Models.Position;

namespace FmuApiApplication.Tests;

public class GisMtConnectTapServiceTests
{
    private const string MarkCode = "0104603334003509215EQ26tdHwAvn\u001d93ee1c";

    private static TapBeerOperation Operation(string uid = "uid-1", string inn = "2464122182") => new()
    {
        Uid = uid,
        Type = "connect_tap",
        Position = new BeerTapPosition
        {
            MarkingCode = MarkCode,
            Id = "123",
            Text = "Пиво",
            Organisation = new Organization { Inn = inn, Kpp = "246401001" }
        }
    };

    [Fact]
    public async Task При_выключенном_журнале_акт_не_создаётся()
    {
        var repository = new FakeJournalRepository();
        var service = GisMtTestData.ConnectTapService(repository, documentsEnabled: false, autoCreate: true, autoSend: true);

        var result = await service.CreateFromTap(Operation());

        Assert.True(result.IsSuccess);
        Assert.Empty(repository.Saved);
    }

    [Fact]
    public async Task Акт_создаётся_с_КИ_и_датой_документа()
    {
        var repository = new FakeJournalRepository();
        var service = GisMtTestData.ConnectTapService(repository, documentsEnabled: true, autoCreate: true, autoSend: false);

        var result = await service.CreateFromTap(Operation());

        Assert.True(result.IsSuccess);
        var document = Assert.Single(repository.Saved);
        Assert.Equal(GisMtJournalKinds.ConnectTap, document.Kind);
        Assert.Equal(GisMtJournalStatuses.Draft, document.Status);
        Assert.Equal("2464122182", document.Inn);
        Assert.Equal("246401001", document.Kpp);
        Assert.Equal("uid-1", document.Uid);
        Assert.Equal("0104603334003509215EQ26tdHwAvn", Assert.Single(document.Lines).Cis);
    }

    [Fact]
    public async Task Повтор_uid_не_создаёт_второй_акт_и_не_отправляет_снова()
    {
        var repository = new FakeJournalRepository();
        var client = new FakeLkClient();
        var service = GisMtTestData.ConnectTapService(repository, documentsEnabled: true, autoCreate: true, autoSend: true, client: client);

        await service.CreateFromTap(Operation());
        await service.CreateFromTap(Operation());

        Assert.Single(repository.Saved);
        Assert.Equal(1, client.Calls);
    }

    [Fact]
    public async Task Без_ИНН_акт_создаётся_с_ошибкой_и_не_отправляется()
    {
        var repository = new FakeJournalRepository();
        var client = new FakeLkClient();
        var service = GisMtTestData.ConnectTapService(repository, documentsEnabled: true, autoCreate: true, autoSend: true, client: client);

        await service.CreateFromTap(Operation(inn: ""));

        var document = Assert.Single(repository.Saved);
        Assert.Equal(GisMtJournalStatuses.Error, document.Status);
        Assert.Contains("ИНН", document.ErrorText);
        Assert.Equal(0, client.Calls);
    }

    [Fact]
    public async Task Неизвестная_организация_даёт_акт_с_ошибкой()
    {
        var repository = new FakeJournalRepository();
        var service = GisMtTestData.ConnectTapService(repository, documentsEnabled: true, autoCreate: true, autoSend: false);

        await service.CreateFromTap(Operation(inn: "1111111111"));

        var document = Assert.Single(repository.Saved);
        Assert.Equal(GisMtJournalStatuses.Error, document.Status);
        Assert.Contains("не найдена", document.ErrorText);
    }

    [Fact]
    public async Task Пустой_КИ_даёт_акт_с_ошибкой()
    {
        var repository = new FakeJournalRepository();
        var service = GisMtTestData.ConnectTapService(repository, documentsEnabled: true, autoCreate: true, autoSend: false);

        var operation = Operation();
        operation.Position.MarkingCode = "не марка";

        await service.CreateFromTap(operation);

        var document = Assert.Single(repository.Saved);
        Assert.Equal(GisMtJournalStatuses.Error, document.Status);
        Assert.Contains("КИ", document.ErrorText);
    }

    [Fact]
    public async Task Автоматическая_отправка_включается_только_третьим_флагом()
    {
        var repository = new FakeJournalRepository();
        var client = new FakeLkClient();
        var service = GisMtTestData.ConnectTapService(repository, documentsEnabled: true, autoCreate: true, autoSend: false, client: client);

        await service.CreateFromTap(Operation());

        Assert.Equal(GisMtJournalStatuses.Draft, Assert.Single(repository.Saved).Status);
        Assert.Equal(0, client.Calls);
    }
}
