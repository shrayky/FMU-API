using FmuApiDomain.Configuration.Options;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Journal;
using FmuApiDomain.GisMt.Models;
using Xunit;

namespace FmuApiApplication.Tests;

public class GisMtJournalServiceTests
{
    [Fact]
    public async Task Успешная_отправка_переводит_акт_в_отправлен()
    {
        var repository = new FakeJournalRepository();
        var document = GisMtTestData.ConnectTapDocument();
        await repository.Save(document);

        var service = GisMtTestData.JournalService(repository, new FakeSigner(), new FakeLkClient());

        var result = await service.Send(document.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(GisMtJournalStatuses.Sent, result.Value.Status);
        Assert.Equal("crpt-1", result.Value.CrptDocumentId);
        Assert.NotNull(result.Value.SentAt);
    }

    [Fact]
    public async Task Ошибка_подписи_оставляет_акт_с_ошибкой_и_текстом()
    {
        var repository = new FakeJournalRepository();
        var document = GisMtTestData.ConnectTapDocument();
        await repository.Save(document);

        var service = GisMtTestData.JournalService(repository, new FakeSigner(failure: "нет сертификата"), new FakeLkClient());

        var result = await service.Send(document.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(GisMtJournalStatuses.Error, document.Status);
        Assert.Equal("нет сертификата", document.ErrorText);
    }

    [Fact]
    public async Task Ошибка_HTTP_оставляет_акт_с_ошибкой_и_текстом()
    {
        var repository = new FakeJournalRepository();
        var document = GisMtTestData.ConnectTapDocument();
        await repository.Save(document);

        var service = GisMtTestData.JournalService(repository, new FakeSigner(), new FakeLkClient(failure: "HTTP 500"));

        var result = await service.Send(document.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(GisMtJournalStatuses.Error, document.Status);
        Assert.Equal("HTTP 500", document.ErrorText);
        Assert.Equal("HTTP 500", result.Error);
    }

    [Fact]
    public async Task Ошибка_ЧЗ_не_отдаётся_пока_статус_ошибки_не_записан()
    {
        var repository = new FakeJournalRepository();
        var document = GisMtTestData.ConnectTapDocument();
        await repository.Save(document);

        repository.SaveFilter = entity => entity.Status != GisMtJournalStatuses.Error;

        var service = GisMtTestData.JournalService(repository, new FakeSigner(), new FakeLkClient(failure: "HTTP 500"));

        var result = await service.Send(document.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("Не удалось сохранить ошибку отправки акта журнала ГИС МТ", result.Error);
        Assert.DoesNotContain("HTTP 500", result.Error);
    }

    [Fact]
    public async Task Отправленный_акт_повторно_не_отправляется()
    {
        var repository = new FakeJournalRepository();
        var document = GisMtTestData.ConnectTapDocument();
        document.Status = GisMtJournalStatuses.Sent;
        await repository.Save(document);

        var client = new FakeLkClient();
        var service = GisMtTestData.JournalService(repository, new FakeSigner(), client);

        var result = await service.Send(document.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(0, client.Calls);
    }

    [Fact]
    public async Task Акт_без_марок_не_отправляется()
    {
        var repository = new FakeJournalRepository();
        var document = GisMtTestData.ConnectTapDocument();
        document.Lines.Clear();
        await repository.Save(document);

        var client = new FakeLkClient();
        var service = GisMtTestData.JournalService(repository, new FakeSigner(), client);

        var result = await service.Send(document.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(0, client.Calls);
        Assert.Equal(GisMtJournalStatuses.Draft, document.Status);
    }

    [Fact]
    public async Task Отправленный_акт_удалить_нельзя()
    {
        var repository = new FakeJournalRepository();
        var document = GisMtTestData.ConnectTapDocument();
        document.Status = GisMtJournalStatuses.Sent;
        await repository.Save(document);

        var service = GisMtTestData.JournalService(repository, new FakeSigner(), new FakeLkClient());

        var result = await service.Delete(document.Id);

        Assert.True(result.IsFailure);
        Assert.Single(repository.Saved);
    }

    [Fact]
    public async Task Черновик_удаляется()
    {
        var repository = new FakeJournalRepository();
        var document = GisMtTestData.ConnectTapDocument();
        await repository.Save(document);

        var service = GisMtTestData.JournalService(repository, new FakeSigner(), new FakeLkClient());

        var result = await service.Delete(document.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(repository.Saved);
    }

    [Fact]
    public async Task Повторный_uid_не_создаёт_второй_акт()
    {
        var repository = new FakeJournalRepository();
        var service = GisMtTestData.JournalService(repository, new FakeSigner(), new FakeLkClient());

        var first = await service.Create(GisMtTestData.ConnectTapDocument());
        var second = await service.Create(GisMtTestData.ConnectTapDocument());

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value.Id, second.Value.Id);
        Assert.Single(repository.Saved);
    }

    [Fact]
    public async Task Акт_постановки_на_кран_получает_id_из_uid()
    {
        var repository = new FakeJournalRepository();
        var service = GisMtTestData.JournalService(repository, new FakeSigner(), new FakeLkClient());

        var created = await service.Create(GisMtTestData.ConnectTapDocument("uid-42"));

        Assert.True(created.IsSuccess);
        Assert.Equal(GisMtJournalIds.FromUid("uid-42"), created.Value.Id);
        Assert.Single(repository.Saved);
    }

    [Fact]
    public async Task Без_записанного_статуса_отправки_документ_в_Честный_знак_не_уходит()
    {
        var repository = new FakeJournalRepository();
        var document = GisMtTestData.ConnectTapDocument();
        await repository.Save(document);

        repository.FailStatusClaim = true;

        var client = new FakeLkClient();
        var service = GisMtTestData.JournalService(repository, new FakeSigner(), client);

        var result = await service.Send(document.Id);

        Assert.True(result.IsFailure);
        Assert.Contains("отправ", result.Error);
        Assert.Equal(0, client.Calls);
    }

    [Fact]
    public async Task Проигранная_гонка_за_статус_не_отправляет_документ()
    {
        var repository = new FakeJournalRepository();
        var document = GisMtTestData.ConnectTapDocument();
        await repository.Save(document);

        repository.FailStatusClaim = true;
        var reads = 0;

        repository.GetHook = current =>
        {
            reads++;

            if (reads > 1 && current is not null)
                current.Status = GisMtJournalStatuses.Sending;

            return current;
        };

        var client = new FakeLkClient();
        var service = GisMtTestData.JournalService(repository, new FakeSigner(), client);

        var result = await service.Send(document.Id);

        Assert.True(result.IsFailure);
        Assert.Contains("уже отправляется", result.Error);
        Assert.Equal(0, client.Calls);
    }

    [Fact]
    public async Task Без_записанного_статуса_sent_успех_не_отдаётся()
    {
        var repository = new FakeJournalRepository();
        var document = GisMtTestData.ConnectTapDocument();
        await repository.Save(document);

        repository.SaveFilter = entity => entity.Status != GisMtJournalStatuses.Sent;

        var client = new FakeLkClient();
        var service = GisMtTestData.JournalService(repository, new FakeSigner(), client);

        var result = await service.Send(document.Id);

        Assert.True(result.IsFailure);
        Assert.Contains("не сохранён", result.Error);
        Assert.Contains("crpt-1", result.Error);
        Assert.Equal(1, client.Calls);
    }

    [Fact]
    public async Task Таймаут_после_запроса_не_ставит_статус_ошибки()
    {
        var repository = new FakeJournalRepository();
        var document = GisMtTestData.ConnectTapDocument();
        await repository.Save(document);

        var client = new FakeLkClient(exception: new TaskCanceledException("timeout"));
        var service = GisMtTestData.JournalService(repository, new FakeSigner(), client);

        var result = await service.Send(document.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(GisMtJournalStatuses.Sending, document.Status);
        Assert.Equal(string.Empty, document.CrptDocumentId);
        Assert.Contains("личном кабинете", document.ErrorText);
    }

    [Fact]
    public async Task Ошибка_клиента_после_запроса_не_ставит_статус_ошибки()
    {
        var repository = new FakeJournalRepository();
        var document = GisMtTestData.ConnectTapDocument();
        await repository.Save(document);

        var client = new FakeLkClient(exception: new InvalidOperationException("соединение сброшено"));
        var service = GisMtTestData.JournalService(repository, new FakeSigner(), client);

        var result = await service.Send(document.Id);

        Assert.True(result.IsFailure);
        Assert.NotEqual(GisMtJournalStatuses.Error, document.Status);
        Assert.Contains("соединение сброшено", document.ErrorText);
    }

    [Fact]
    public async Task Неудача_записи_причины_всё_равно_возвращает_неподтверждённую_отправку()
    {
        var repository = new FakeJournalRepository();
        var document = GisMtTestData.ConnectTapDocument();
        await repository.Save(document);

        repository.SaveFilter = entity => !(entity.Status == GisMtJournalStatuses.Sending
            && entity.ErrorText.Contains("личном кабинете"));

        var client = new FakeLkClient(exception: new TaskCanceledException("timeout"));
        var service = GisMtTestData.JournalService(repository, new FakeSigner(), client);

        var result = await service.Send(document.Id);

        Assert.True(result.IsFailure);
        Assert.Contains("личном кабинете", result.Error);
    }

    [Fact]
    public async Task Сверка_привязывает_найденный_документ_ЧЗ()
    {
        var repository = new FakeJournalRepository();
        var document = UnconfirmedDocument();
        await repository.Save(document);

        var documents = new FakeDocumentsClient(new GisMtDocInfoResponse { Status = GisMtCrptStatuses.Accepted });
        var service = GisMtTestData.JournalService(
            repository,
            new FakeSigner(),
            new FakeLkClient(),
            documents: documents);

        var result = await service.ReconcileUnconfirmed(document.Id, " crpt-9 ");

        Assert.True(result.IsSuccess);
        Assert.Equal(GisMtJournalStatuses.Sent, result.Value.Status);
        Assert.Equal("crpt-9", result.Value.CrptDocumentId);
        Assert.NotNull(result.Value.SentAt);
        Assert.Equal(GisMtCrptStatuses.Accepted, result.Value.CrptStatus);
        Assert.Equal(1, documents.InfoCalls);
    }

    [Fact]
    public async Task Сверка_не_привязывает_чужой_документ()
    {
        var repository = new FakeJournalRepository();
        var document = UnconfirmedDocument();
        await repository.Save(document);

        var service = GisMtTestData.JournalService(
            repository,
            new FakeSigner(),
            new FakeLkClient(),
            documents: new FakeDocumentsClient(failure: "doc/info HTTP 404"));

        var result = await service.ReconcileUnconfirmed(document.Id, "crpt-9");

        Assert.True(result.IsFailure);
        Assert.Equal(GisMtJournalStatuses.Sending, document.Status);
        Assert.Equal(string.Empty, document.CrptDocumentId);
    }

    [Fact]
    public async Task Без_документа_в_ЧЗ_акт_возвращается_в_ошибку_и_отправляется_снова()
    {
        var repository = new FakeJournalRepository();
        var document = UnconfirmedDocument();
        await repository.Save(document);

        var client = new FakeLkClient();
        var service = GisMtTestData.JournalService(repository, new FakeSigner(), client);

        var returned = await service.ReturnUnconfirmedToError(document.Id);

        Assert.True(returned.IsSuccess);
        Assert.Equal(GisMtJournalStatuses.Error, document.Status);
        Assert.Contains("не найден", document.ErrorText);

        var sent = await service.Send(document.Id);

        Assert.True(sent.IsSuccess);
        Assert.Equal(1, client.Calls);
    }

    [Fact]
    public async Task Сверка_не_трогает_обычный_черновик()
    {
        var repository = new FakeJournalRepository();
        var document = GisMtTestData.ConnectTapDocument();
        await repository.Save(document);

        var service = GisMtTestData.JournalService(repository, new FakeSigner(), new FakeLkClient());

        var result = await service.ReconcileUnconfirmed(document.Id, "crpt-9");

        Assert.True(result.IsFailure);
        Assert.Equal(GisMtJournalStatuses.Draft, document.Status);
    }

    [Fact]
    public async Task Отправка_списания_по_сроку_годности_помечает_марки_проданными()
    {
        var repository = new FakeJournalRepository();
        var mark = GisMtTestData.ExpiredMark("0104603334003509215ABC", "milk", DateTime.UtcNow.AddDays(-1));
        var marks = new FakeMarksRepository(mark);
        var document = ExpirationDocument(mark);
        await repository.Save(document);

        var service = GisMtTestData.JournalService(repository, new FakeSigner(), new FakeLkClient(), marks: marks);

        var result = await service.Send(document.Id);

        Assert.True(result.IsSuccess);
        Assert.True(mark.Sold);
    }

    [Fact]
    public async Task Неудачная_отправка_списания_по_сроку_годности_не_помечает_марки_проданными()
    {
        var repository = new FakeJournalRepository();
        var mark = GisMtTestData.ExpiredMark("0104603334003509215ABC", "milk", DateTime.UtcNow.AddDays(-1));
        var marks = new FakeMarksRepository(mark);
        var document = ExpirationDocument(mark);
        await repository.Save(document);

        var service = GisMtTestData.JournalService(
            repository,
            new FakeSigner(),
            new FakeLkClient(failure: "HTTP 500"),
            marks: marks);

        var result = await service.Send(document.Id);

        Assert.True(result.IsFailure);
        Assert.False(mark.Sold);
    }

    [Fact]
    public async Task Отправка_фасовки_не_помечает_марки_проданными()
    {
        var repository = new FakeJournalRepository();
        var mark = GisMtTestData.ExpiredMark("0104603334003509215ABC", "milk", DateTime.UtcNow.AddDays(-1));
        var marks = new FakeMarksRepository(mark);
        var document = ExpirationDocument(mark);
        document.Reason = GisMtWriteOffReasons.Packing;
        await repository.Save(document);

        var service = GisMtTestData.JournalService(repository, new FakeSigner(), new FakeLkClient(), marks: marks);

        await service.Send(document.Id);

        Assert.False(mark.Sold);
    }

    [Fact]
    public async Task Отправка_списания_уходит_в_товарную_группу_акта()
    {
        var repository = new FakeJournalRepository();
        var mark = GisMtTestData.ExpiredMark("0104603334003509215ABC", "water", DateTime.UtcNow.AddDays(-1));
        var document = ExpirationDocument(mark);
        document.ProductGroup = "water";
        await repository.Save(document);
        var client = new FakeLkClient();

        var service = GisMtTestData.JournalService(
            repository,
            new FakeSigner(),
            client,
            marks: new FakeMarksRepository(mark));

        var result = await service.Send(document.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("water", client.LastProductGroup);
    }

    [Fact]
    public async Task Отправка_списания_без_группы_уходит_в_молочную_продукцию()
    {
        var repository = new FakeJournalRepository();
        var mark = GisMtTestData.ExpiredMark("0104603334003509215ABC", "milk", DateTime.UtcNow.AddDays(-1));
        var document = ExpirationDocument(mark);
        document.ProductGroup = string.Empty;
        await repository.Save(document);
        var client = new FakeLkClient();

        var service = GisMtTestData.JournalService(
            repository,
            new FakeSigner(),
            client,
            marks: new FakeMarksRepository(mark));

        var result = await service.Send(document.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("milk", client.LastProductGroup);
    }

    [Fact]
    public async Task Статус_документа_сохраняется_в_акте()
    {
        var repository = new FakeJournalRepository();
        var document = SentDocument();
        await repository.Save(document);
        var documents = new FakeDocumentsClient(new GisMtDocInfoResponse { Status = GisMtCrptStatuses.CheckedOk });

        var service = GisMtTestData.JournalService(
            repository,
            new FakeSigner(),
            new FakeLkClient(),
            documents: documents);

        var result = await service.CheckCrptStatus(document.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(GisMtCrptStatuses.CheckedOk, result.Value.CrptStatus);
        Assert.Equal(string.Empty, result.Value.CrptStatusText);
        Assert.NotNull(result.Value.CrptStatusCheckedAt);
        Assert.Equal(1, documents.InfoCalls);
    }

    [Fact]
    public async Task Ошибки_обработки_сохраняются_в_акте()
    {
        var repository = new FakeJournalRepository();
        var document = SentDocument();
        await repository.Save(document);
        var documents = new FakeDocumentsClient(new GisMtDocInfoResponse
        {
            Status = GisMtCrptStatuses.CheckedNotOk,
            Errors = ["КИ не найден в обороте"]
        });

        var service = GisMtTestData.JournalService(
            repository,
            new FakeSigner(),
            new FakeLkClient(),
            documents: documents);

        var result = await service.CheckCrptStatus(document.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(GisMtCrptStatuses.CheckedNotOk, result.Value.CrptStatus);
        Assert.Equal("КИ не найден в обороте", result.Value.CrptStatusText);
    }

    [Fact]
    public async Task Ошибка_запроса_статуса_не_меняет_акт()
    {
        var repository = new FakeJournalRepository();
        var document = SentDocument();
        await repository.Save(document);

        var service = GisMtTestData.JournalService(
            repository,
            new FakeSigner(),
            new FakeLkClient(),
            documents: new FakeDocumentsClient(failure: "doc/info HTTP 401"));

        var result = await service.CheckCrptStatus(document.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(string.Empty, document.CrptStatus);
    }

    [Fact]
    public async Task Без_идентификатора_ЧЗ_статус_не_запрашивается()
    {
        var repository = new FakeJournalRepository();
        var document = GisMtTestData.ConnectTapDocument();
        await repository.Save(document);
        var documents = new FakeDocumentsClient();

        var service = GisMtTestData.JournalService(
            repository,
            new FakeSigner(),
            new FakeLkClient(),
            documents: documents);

        var result = await service.CheckCrptStatus(document.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(0, documents.InfoCalls);
    }

    [Fact]
    public async Task Отказ_ЧЗ_возвращает_марки_в_остатки()
    {
        var repository = new FakeJournalRepository();
        var mark = GisMtTestData.ExpiredMark("0104603334003509215ABC", "water", DateTime.UtcNow.AddDays(-1));
        var marks = new FakeMarksRepository(mark);
        var document = ExpirationDocument(mark);
        await repository.Save(document);

        var service = GisMtTestData.JournalService(
            repository,
            new FakeSigner(),
            new FakeLkClient(),
            marks: marks,
            documents: new FakeDocumentsClient(new GisMtDocInfoResponse
            {
                Status = GisMtCrptStatuses.CheckedNotOk,
                Errors = ["25: Коды идентификации не принадлежат товарной группе \"Молочная продукция\", указанной в документе."]
            }));

        await service.Send(document.Id);

        Assert.True(mark.Sold);

        var status = await service.CheckCrptStatus(document.Id);

        Assert.True(status.IsSuccess);
        Assert.False(mark.Sold);
        Assert.Contains("не принадлежат товарной группе", status.Value.CrptStatusText);
    }

    [Fact]
    public async Task Отказ_ЧЗ_не_возвращает_марки_если_статус_не_записан()
    {
        var repository = new FakeJournalRepository();
        var mark = GisMtTestData.ExpiredMark("0104603334003509215ABC", "water", DateTime.UtcNow.AddDays(-1));
        var marks = new FakeMarksRepository(mark);
        var document = ExpirationDocument(mark);
        await repository.Save(document);

        var service = GisMtTestData.JournalService(
            repository,
            new FakeSigner(),
            new FakeLkClient(),
            marks: marks,
            documents: new FakeDocumentsClient(new GisMtDocInfoResponse
            {
                Status = GisMtCrptStatuses.CheckedNotOk,
                Errors = ["25: Коды идентификации не принадлежат товарной группе \"Молочная продукция\", указанной в документе."]
            }));

        await service.Send(document.Id);

        Assert.True(mark.Sold);

        repository.SaveFilter = entity => string.IsNullOrEmpty(entity.CrptStatus);

        var status = await service.CheckCrptStatus(document.Id);

        Assert.True(status.IsFailure);
        Assert.Contains("сохранить статус", status.Error);
        Assert.True(mark.Sold);
    }

    [Fact]
    public async Task Успешная_обработка_оставляет_марки_проданными()
    {
        var repository = new FakeJournalRepository();
        var mark = GisMtTestData.ExpiredMark("0104603334003509215ABC", "water", DateTime.UtcNow.AddDays(-1));
        var marks = new FakeMarksRepository(mark);
        var document = ExpirationDocument(mark);
        await repository.Save(document);

        var service = GisMtTestData.JournalService(
            repository,
            new FakeSigner(),
            new FakeLkClient(),
            marks: marks,
            documents: new FakeDocumentsClient(new GisMtDocInfoResponse { Status = GisMtCrptStatuses.CheckedOk }));

        await service.Send(document.Id);
        await service.CheckCrptStatus(document.Id);

        Assert.True(mark.Sold);
    }

    [Fact]
    public async Task Документ_с_повторяющимися_КИ_не_отправляется()
    {
        var repository = new FakeJournalRepository();
        var document = GisMtTestData.ConnectTapDocument();
        document.Lines =
        [
            new GisMtJournalLine { Cis = "0104603334003509215ABC" },
            new GisMtJournalLine { Cis = "0104603334003509215ABC" }
        ];
        await repository.Save(document);
        var client = new FakeLkClient();

        var service = GisMtTestData.JournalService(repository, new FakeSigner(), client);

        var result = await service.Send(document.Id);

        Assert.True(result.IsFailure);
        Assert.Contains("повторяются КИ", result.Error);
        Assert.Equal(0, client.Calls);
        Assert.Equal(GisMtJournalStatuses.Error, document.Status);
    }

    [Fact]
    public async Task Отклонённый_ЧЗ_документ_удаляется()
    {
        var repository = new FakeJournalRepository();
        var document = GisMtTestData.ConnectTapDocument();
        document.Status = GisMtJournalStatuses.Sent;
        document.CrptStatus = GisMtCrptStatuses.CheckedNotOk;
        await repository.Save(document);

        var service = GisMtTestData.JournalService(repository, new FakeSigner(), new FakeLkClient());

        var result = await service.Delete(document.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(repository.Saved);
    }

    [Fact]
    public async Task Принятый_ЧЗ_документ_удалить_нельзя()
    {
        var repository = new FakeJournalRepository();
        var document = GisMtTestData.ConnectTapDocument();
        document.Status = GisMtJournalStatuses.Sent;
        document.CrptStatus = GisMtCrptStatuses.CheckedOk;
        await repository.Save(document);

        var service = GisMtTestData.JournalService(repository, new FakeSigner(), new FakeLkClient());

        var result = await service.Delete(document.Id);

        Assert.True(result.IsFailure);
        Assert.Single(repository.Saved);
    }

    [Fact]
    public async Task Отправляемый_акт_не_отправляется_повторно()
    {
        var repository = new FakeJournalRepository();
        var document = GisMtTestData.ConnectTapDocument();
        document.Status = GisMtJournalStatuses.Sending;
        await repository.Save(document);
        var client = new FakeLkClient();

        var service = GisMtTestData.JournalService(repository, new FakeSigner(), client);

        var result = await service.Send(document.Id);

        Assert.True(result.IsFailure);
        Assert.Contains("отправляется", result.Error);
        Assert.Equal(0, client.Calls);
    }

    [Fact]
    public async Task Отправляемый_акт_удалить_нельзя()
    {
        var repository = new FakeJournalRepository();
        var document = GisMtTestData.ConnectTapDocument();
        document.Status = GisMtJournalStatuses.Sending;
        await repository.Save(document);

        var service = GisMtTestData.JournalService(repository, new FakeSigner(), new FakeLkClient());

        var result = await service.Delete(document.Id);

        Assert.True(result.IsFailure);
        Assert.Single(repository.Saved);
    }

    [Fact]
    public async Task Фоновый_опрос_берёт_только_ожидающие_статуса()
    {
        var repository = new FakeJournalRepository();
        var pending = ExpirationDocument(GisMtTestData.ExpiredMark(Cis(1), "water", DateTime.UtcNow.AddDays(-1)));
        var finished = ExpirationDocument(GisMtTestData.ExpiredMark(Cis(2), "water", DateTime.UtcNow.AddDays(-1)));
        pending.Status = GisMtJournalStatuses.Sent;
        pending.CrptDocumentId = "crpt-1";
        finished.Status = GisMtJournalStatuses.Sent;
        finished.CrptDocumentId = "crpt-2";
        finished.CrptStatus = GisMtCrptStatuses.CheckedOk;

        await repository.Save(pending);
        await repository.Save(finished);

        var documents = new FakeDocumentsClient(new GisMtDocInfoResponse { Status = GisMtCrptStatuses.CheckedOk });
        var parameters = GisMtTestData.Parameters();
        parameters.GisMtSettings = new GisMtSettings { DocumentsEnabled = true };
        var service = GisMtTestData.JournalService(
            repository,
            new FakeSigner(),
            new FakeLkClient(),
            parameters,
            documents: documents);

        var result = await service.CheckPendingCrptStatuses();

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value);
        Assert.Equal(1, documents.InfoCalls);
        Assert.Equal(GisMtCrptStatuses.CheckedOk, pending.CrptStatus);
    }

    private static string Cis(int serial) => $"010460333400350921{serial:D6}";

    [Fact]
    public async Task Отклонённые_коды_разбираются_с_упаковкой()
    {
        var repository = new FakeJournalRepository();
        var document = SentDocument();
        await repository.Save(document);

        var documents = new FakeDocumentsClient(new GisMtDocInfoResponse
        {
            Status = GisMtCrptStatuses.CheckedNotOk,
            Errors = ["16: Код идентификации 0104607022743036215Zmxc\"&QcDtjd не уникален в документе."]
        });

        var history = new FakeCisHistoryClient(new GisMtCisHistoryRecord
        {
            Cis = "0104607022743036215Zmxc\"&QcDtjd",
            Status = "INTRODUCED",
            PackageType = "UNIT",
            Parent = "0104607022743043215ATKSSh;wU>IV"
        });

        var service = GisMtTestData.JournalService(
            repository,
            new FakeSigner(),
            new FakeLkClient(),
            documents: documents,
            history: history);

        var result = await service.CheckCrptStatus(document.Id);

        Assert.True(result.IsSuccess);
        Assert.Contains("ATKSSh;wU>IV", result.Value.CrptErrorsDetail);
        Assert.Equal(1, history.Calls);
    }

    private static GisMtJournalEntity UnconfirmedDocument() => new()
    {
        Id = Guid.NewGuid().ToString(),
        Kind = GisMtJournalKinds.ConnectTap,
        Status = GisMtJournalStatuses.Sending,
        Inn = "2464122182",
        Kpp = "246401001",
        Uid = "uid-1",
        DocDate = new DateOnly(2026, 11, 10),
        CreatedAt = DateTime.Now,
        ErrorText = "Ответ Честного знака не получен",
        Lines = [new GisMtJournalLine { Cis = "0104603334003509215A" }]
    };

    private static GisMtJournalEntity SentDocument() => new()
    {
        Id = Guid.NewGuid().ToString(),
        Kind = GisMtJournalKinds.LkReceipt,
        Reason = GisMtWriteOffReasons.Expiration,
        Status = GisMtJournalStatuses.Sent,
        Inn = "2464122182",
        Kpp = "246401001",
        DocDate = new DateOnly(2026, 11, 10),
        CreatedAt = DateTime.Now,
        SentAt = DateTime.Now,
        CrptDocumentId = "crpt-1",
        Lines = [new GisMtJournalLine { Mark = "sgtin-1", Cis = "0104603334003509215A" }]
    };

    private static GisMtJournalEntity ExpirationDocument(GisMtMarkEntity mark) => new()
    {
        Id = Guid.NewGuid().ToString(),
        Kind = GisMtJournalKinds.LkReceipt,
        Reason = GisMtWriteOffReasons.Expiration,
        Status = GisMtJournalStatuses.Draft,
        Inn = "2464122182",
        Kpp = "246401001",
        DocDate = new DateOnly(2026, 11, 10),
        CreatedAt = DateTime.Now,
        Lines = [new GisMtJournalLine { Mark = mark.Id, Cis = mark.Cis, Gtin = mark.Gtin }]
    };
}
