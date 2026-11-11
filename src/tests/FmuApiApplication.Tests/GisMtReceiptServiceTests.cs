using FmuApiDomain.GisMt.Journal;
using FmuApiDomain.GisMt.Models;
using Xunit;

namespace FmuApiApplication.Tests;

public class GisMtReceiptServiceTests
{
    private const string MarkWithWeight = "0104603334003509215EQ26tdHwAvn\u001d93ee1c";
    private const string MarkWithoutWeight = "0104603334003509216EQ26tdHwAvn\u001d93ee1c";

    [Fact]
    public async Task Строка_получает_вес_цену_и_сумму()
    {
        var repository = new FakeJournalRepository();
        var service = GisMtTestData.ReceiptService(
            repository,
            new Dictionary<string, int> { ["0104603334003509215EQ26tdHwAvn"] = 500 },
            250m);

        var result = await service.Create("2464122182", [MarkWithWeight]);

        Assert.True(result.IsSuccess);
        var line = Assert.Single(result.Value.Document.Lines);
        Assert.Equal(0.5m, line.QuantityKg);
        Assert.Equal(250m, line.PricePerKg);
        Assert.Equal(12500, line.AmountKop);
        Assert.Empty(result.Value.LineErrors);
    }

    [Fact]
    public async Task Без_веса_сумма_пуста_а_строка_сохраняется()
    {
        var repository = new FakeJournalRepository();
        var service = GisMtTestData.ReceiptService(repository, [], 250m);

        var result = await service.Create("2464122182", [MarkWithoutWeight]);

        Assert.True(result.IsSuccess);
        var line = Assert.Single(result.Value.Document.Lines);
        Assert.Null(line.QuantityKg);
        Assert.Null(line.AmountKop);
    }

    [Fact]
    public async Task Без_цены_сумма_пуста()
    {
        var repository = new FakeJournalRepository();
        var service = GisMtTestData.ReceiptService(
            repository,
            new Dictionary<string, int> { ["0104603334003509215EQ26tdHwAvn"] = 500 },
            null);

        var result = await service.Create("2464122182", [MarkWithWeight]);

        var line = Assert.Single(result.Value.Document.Lines);
        Assert.Null(line.PricePerKg);
        Assert.Null(line.AmountKop);
    }

    [Fact]
    public async Task Марка_без_КИ_не_попадает_в_документ_и_даёт_ошибку()
    {
        var repository = new FakeJournalRepository();
        var service = GisMtTestData.ReceiptService(repository, [], null);

        var result = await service.Create("2464122182", [MarkWithWeight, "не марка"]);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Document.Lines);
        Assert.Single(result.Value.LineErrors);
    }

    [Fact]
    public async Task Без_единой_валидной_марки_документ_не_создаётся()
    {
        var repository = new FakeJournalRepository();
        var service = GisMtTestData.ReceiptService(repository, [], null);

        var result = await service.Create("2464122182", ["не марка"]);

        Assert.True(result.IsFailure);
        Assert.Empty(repository.Saved);
    }

    [Fact]
    public async Task Документ_создаётся_черновиком()
    {
        var repository = new FakeJournalRepository();
        var service = GisMtTestData.ReceiptService(repository, [], null);

        var result = await service.Create("2464122182", [MarkWithWeight]);

        Assert.Equal(GisMtJournalKinds.LkReceipt, result.Value.Document.Kind);
        Assert.Equal(GisMtJournalStatuses.Draft, result.Value.Document.Status);
        Assert.Equal(string.Empty, result.Value.Document.Uid);
    }

    [Fact]
    public async Task Предпросмотр_считает_КИ_вес_цену_и_сумму()
    {
        var repository = new FakeJournalRepository();
        var service = GisMtTestData.ReceiptService(
            repository,
            new Dictionary<string, int> { ["0104603334003509215EQ26tdHwAvn"] = 500 },
            250m);

        var result = await service.Preview("2464122182", [MarkWithWeight, "не марка"]);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);

        var line = result.Value[0];
        Assert.Equal("0104603334003509215EQ26tdHwAvn", line.Cis);
        Assert.Equal("04603334003509", line.Gtin);
        Assert.Equal("Молоко пастеризованное 3,2%", line.ProductName);
        Assert.Equal(0.5m, line.QuantityKg);
        Assert.Equal(250m, line.PricePerKg);
        Assert.Equal(12500, line.AmountKop);
        Assert.Equal(string.Empty, line.Error);

        Assert.Contains("не разбирается в КИ", result.Value[1].Error);
    }

    [Fact]
    public async Task Черновик_редактируется_и_остаётся_тем_же_документом()
    {
        var repository = new FakeJournalRepository();
        var service = GisMtTestData.ReceiptService(
            repository,
            new Dictionary<string, int> { ["0104603334003509215EQ26tdHwAvn"] = 500 },
            250m);

        var created = await service.Create("2464122182", [MarkWithWeight]);
        var documentId = created.Value.Document.Id;

        var updated = await service.Update(documentId, "2464122182", [MarkWithWeight, MarkWithoutWeight]);

        Assert.True(updated.IsSuccess);
        Assert.Equal(documentId, updated.Value.Document.Id);
        Assert.Equal(2, updated.Value.Document.Lines.Count);
        Assert.Equal(GisMtJournalStatuses.Draft, updated.Value.Document.Status);
    }

    [Fact]
    public async Task Отправленный_документ_не_редактируется()
    {
        var repository = new FakeJournalRepository();
        var service = GisMtTestData.ReceiptService(repository, [], null);

        var created = await service.Create("2464122182", [MarkWithWeight]);
        created.Value.Document.Status = GisMtJournalStatuses.Sent;
        await repository.Save(created.Value.Document);

        var updated = await service.Update(created.Value.Document.Id, "2464122182", [MarkWithWeight]);

        Assert.True(updated.IsFailure);
        Assert.Contains("Отправленный", updated.Error);
    }

    [Fact]
    public async Task Правка_черновика_берёт_КИ_из_сохранённой_строки_если_марка_без_GS()
    {
        var repository = new FakeJournalRepository();
        var stored = new FmuApiDomain.GisMt.Entities.GisMtJournalEntity
        {
            Id = "doc-1",
            Kind = GisMtJournalKinds.LkReceipt,
            Status = GisMtJournalStatuses.Draft,
            Inn = "2464122182",
            Kpp = "246401001",
            DocDate = new DateOnly(2026, 11, 2),
            CreatedAt = DateTime.Now,
            Lines =
            [
                new FmuApiDomain.GisMt.Entities.GisMtJournalLine
                {
                    Mark = "0104610050691842215NHGPh",
                    Cis = "0104610050691842215NH",
                    Gtin = "04610050691842",
                    ProductName = "Молоко",
                    QuantityKg = 0.9m,
                    PricePerKg = 100m,
                    AmountKop = 9000
                }
            ]
        };

        await repository.Save(stored);

        var service = GisMtTestData.ReceiptService(repository, [], null);

        var updated = await service.Update("doc-1", "2464122182", ["0104610050691842215NHGPh"]);

        Assert.True(updated.IsSuccess);
        var line = Assert.Single(updated.Value.Document.Lines);
        Assert.Equal("0104610050691842215NH", line.Cis);
        Assert.Equal(9000, line.AmountKop);
        Assert.Empty(updated.Value.LineErrors);
    }

    [Fact]
    public async Task Неизвестная_организация_даёт_отказ()
    {
        var repository = new FakeJournalRepository();
        var service = GisMtTestData.ReceiptService(repository, [], null);

        var result = await service.Create("1111111111", [MarkWithWeight]);

        Assert.True(result.IsFailure);
        Assert.Empty(repository.Saved);
    }

    [Fact]
    public async Task Списание_по_сроку_годности_берёт_КИ_из_строки_подбора()
    {
        var repository = new FakeJournalRepository();
        var service = GisMtTestData.ReceiptService(repository, [], null);

        var result = await service.CreateExpiration(
            "2464122182",
            "milk",
            [new GisMtExpiredLine("04603334003509215DRAreEJwtMz", "0104603334003509215DRAreEJwtMz")]);

        Assert.True(result.IsSuccess);
        var document = result.Value.Document;
        Assert.Equal(GisMtJournalKinds.LkReceipt, document.Kind);
        Assert.Equal(GisMtWriteOffReasons.Expiration, document.Reason);
        Assert.Equal("milk", document.ProductGroup);
        Assert.Equal(GisMtJournalStatuses.Draft, document.Status);

        var line = Assert.Single(document.Lines);
        Assert.Equal("0104603334003509215DRAreEJwtMz", line.Cis);
        Assert.Equal("04603334003509", line.Gtin);
        Assert.Null(line.AmountKop);
        Assert.Empty(result.Value.LineErrors);
    }

    [Fact]
    public async Task Строка_без_КИ_не_попадает_в_списание_по_сроку_годности()
    {
        var repository = new FakeJournalRepository();
        var service = GisMtTestData.ReceiptService(repository, [], null);

        var result = await service.CreateExpiration(
            "2464122182",
            "milk",
            [
                new GisMtExpiredLine("sgtin-1", "0104603334003509215DRAreEJwtMz"),
                new GisMtExpiredLine("sgtin-2", "не марка")
            ]);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Document.Lines);
        Assert.Single(result.Value.LineErrors);
    }

    [Fact]
    public async Task Списание_по_сроку_годности_сохраняет_товарную_группу()
    {
        var repository = new FakeJournalRepository();
        var service = GisMtTestData.ReceiptService(repository, [], null);

        var result = await service.CreateExpiration(
            "2464122182",
            "water",
            [new GisMtExpiredLine("sgtin-1", "0104603334003509215DRAreEJwtMz")]);

        Assert.True(result.IsSuccess);
        Assert.Equal("water", result.Value.Document.ProductGroup);
    }

    [Fact]
    public async Task Без_товарной_группы_списание_по_сроку_годности_не_создаётся()
    {
        var repository = new FakeJournalRepository();
        var service = GisMtTestData.ReceiptService(repository, [], null);

        var result = await service.CreateExpiration(
            "2464122182",
            " ",
            [new GisMtExpiredLine("sgtin-1", "0104603334003509215DRAreEJwtMz")]);

        Assert.True(result.IsFailure);
        Assert.Contains("товарную группу", result.Error);
        Assert.Empty(repository.Saved);
    }

    [Fact]
    public async Task Заполнение_берёт_просроченные_непроданные_марки_одной_группы_до_пятисот()
    {
        var marks = Enumerable
            .Range(0, 600)
            .Select(index => GisMtTestData.ExpiredMark(Cis(index), "milk", DateTime.UtcNow.AddDays(-1).AddMinutes(-index)))
            .ToList();

        marks.Add(GisMtTestData.ExpiredMark(Cis(900), "water", DateTime.UtcNow.AddDays(-1)));
        marks.Add(GisMtTestData.ExpiredMark(Cis(901), "milk", DateTime.UtcNow.AddDays(-1), sold: true));
        marks.Add(GisMtTestData.ExpiredMark(Cis(902), "milk", DateTime.UtcNow.AddDays(30)));

        var repository = new FakeJournalRepository();
        var service = GisMtTestData.ReceiptService(repository, [], null, new FakeMarksRepository([.. marks]));

        var filled = await service.FillExpired("2464122182", "milk");

        Assert.True(filled.IsSuccess);
        Assert.Equal(GisMtJournalLimits.MaxExpiredMarksPerFill, filled.Value.Marks.Count);
        Assert.All(filled.Value.Marks, mark => Assert.StartsWith("0104603334003509", mark.Cis));

        var groups = await service.Groups("2464122182");

        Assert.True(groups.IsSuccess);
        Assert.Equal(new[] { "milk", "water" }, groups.Value);
    }

    [Fact]
    public async Task Подбор_пропускает_марки_из_уже_отправленных_актов()
    {
        var sentMark = GisMtTestData.ExpiredMark(Cis(1), "water", DateTime.UtcNow.AddDays(-1));
        var freeMark = GisMtTestData.ExpiredMark(Cis(2), "water", DateTime.UtcNow.AddDays(-1));

        var repository = new FakeJournalRepository();
        await repository.Save(new FmuApiDomain.GisMt.Entities.GisMtJournalEntity
        {
            Id = "sent-1",
            Kind = GisMtJournalKinds.LkReceipt,
            Reason = GisMtWriteOffReasons.Expiration,
            ProductGroup = "water",
            Status = GisMtJournalStatuses.Sent,
            Inn = "2464122182",
            CreatedAt = DateTime.Now,
            Lines = [new FmuApiDomain.GisMt.Entities.GisMtJournalLine { Cis = sentMark.Cis }]
        });

        var service = GisMtTestData.ReceiptService(repository, [], null, new FakeMarksRepository(sentMark, freeMark));

        var filled = await service.FillExpired("2464122182", "water");

        Assert.True(filled.IsSuccess);
        var mark = Assert.Single(filled.Value.Marks);
        Assert.Equal(freeMark.Cis, mark.Cis);
    }

    [Fact]
    public async Task Подбор_отказывает_если_отправленные_КИ_не_прочитаны()
    {
        var mark = GisMtTestData.ExpiredMark(Cis(1), "water", DateTime.UtcNow.AddDays(-1));
        var repository = new FakeJournalRepository { SubmittedCisError = "Ошибка запроса к БД" };
        var service = GisMtTestData.ReceiptService(repository, [], null, new FakeMarksRepository(mark));

        var filled = await service.FillExpired("2464122182", "water");

        Assert.True(filled.IsFailure);
        Assert.Equal("Ошибка запроса к БД", filled.Error);
    }

    [Fact]
    public async Task Заполнение_без_товарной_группы_даёт_отказ()
    {
        var repository = new FakeJournalRepository();
        var service = GisMtTestData.ReceiptService(repository, [], null);

        var filled = await service.FillExpired("2464122182", " ");

        Assert.True(filled.IsFailure);
        Assert.Contains("товарную группу", filled.Error);
    }

    [Fact]
    public async Task Правка_черновика_по_сроку_годности_меняет_строки()
    {
        var repository = new FakeJournalRepository();
        var service = GisMtTestData.ReceiptService(repository, [], null);

        var created = await service.CreateExpiration(
            "2464122182",
            "milk",
            [new GisMtExpiredLine("sgtin-1", "0104603334003509215DRAreEJwtMz")]);

        var updated = await service.UpdateExpiration(
            created.Value.Document.Id,
            "2464122182",
            "water",
            [
                new GisMtExpiredLine("sgtin-1", "0104603334003509215DRAreEJwtMz"),
                new GisMtExpiredLine("sgtin-2", "0104603334003509216DRAreEJwtMz")
            ]);

        Assert.True(updated.IsSuccess);
        Assert.Equal(created.Value.Document.Id, updated.Value.Document.Id);
        Assert.Equal(2, updated.Value.Document.Lines.Count);
        Assert.Equal(GisMtWriteOffReasons.Expiration, updated.Value.Document.Reason);
        Assert.Equal("water", updated.Value.Document.ProductGroup);
    }

    [Fact]
    public async Task Повтор_КИ_в_подборе_попадает_в_документ_один_раз()
    {
        var repository = new FakeJournalRepository();
        var service = GisMtTestData.ReceiptService(repository, [], null);

        var result = await service.CreateExpiration(
            "2464122182",
            "water",
            [
                new GisMtExpiredLine("sgtin-1", "0104607022743036215hJZ5k"),
                new GisMtExpiredLine("sgtin-2", "0104607022743036215hJZ5k"),
                new GisMtExpiredLine("sgtin-3", "0104607022743036215hJZ5k\u001d93abcd")
            ]);

        Assert.True(result.IsSuccess);
        var line = Assert.Single(result.Value.Document.Lines);
        Assert.Equal("0104607022743036215hJZ5k", line.Cis);
    }

    [Fact]
    public async Task Подбор_не_повторяет_одну_и_ту_же_марку()
    {
        var marks = new[]
        {
            GisMtTestData.ExpiredMark(Cis(1), "water", DateTime.UtcNow.AddDays(-1)),
            GisMtTestData.ExpiredMark(Cis(1), "water", DateTime.UtcNow.AddDays(-2))
        };

        var repository = new FakeJournalRepository();
        var service = GisMtTestData.ReceiptService(repository, [], null, new FakeMarksRepository(marks));

        var filled = await service.FillExpired("2464122182", "water");

        Assert.True(filled.IsSuccess);
        Assert.Single(filled.Value.Marks);
    }

    [Fact]
    public async Task Отправляемый_черновик_не_редактируется()
    {
        var repository = new FakeJournalRepository();
        var service = GisMtTestData.ReceiptService(repository, [], null);

        var created = await service.CreateExpiration(
            "2464122182",
            "milk",
            [new GisMtExpiredLine("sgtin-1", "0104603334003509215DRAreEJwtMz")]);

        created.Value.Document.Status = GisMtJournalStatuses.Sending;
        await repository.Save(created.Value.Document);

        var updated = await service.UpdateExpiration(
            created.Value.Document.Id,
            "2464122182",
            "milk",
            [new GisMtExpiredLine("sgtin-1", "0104603334003509215DRAreEJwtMz")]);

        Assert.True(updated.IsFailure);
        Assert.Contains("отправляется", updated.Error);
    }

    [Fact]
    public async Task КИ_с_хвостом_сжимается_до_короткого_кода()
    {
        var repository = new FakeJournalRepository();
        var service = GisMtTestData.ReceiptService(repository, [], null);

        var result = await service.CreateExpiration(
            "2464122182",
            "water",
            [
                new GisMtExpiredLine("sgtin-1", "0104607022743036215+/S3Wr(ItfAR"),
                new GisMtExpiredLine("sgtin-2", "0104607022743036215+/S3Wr(ItfARxyz"),
                new GisMtExpiredLine("sgtin-3", "0104607022743036215+/S3Wr(ItfAX")
            ]);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Document.Lines.Count);
        Assert.Contains(result.Value.Document.Lines, line => line.Cis == "0104607022743036215+/S3Wr(ItfAR");
        Assert.Contains(result.Value.Document.Lines, line => line.Cis == "0104607022743036215+/S3Wr(ItfAX");
    }

    [Fact]
    public async Task Подбор_исключает_упаковку_чьи_вложения_уже_в_подборе()
    {
        var unit = GisMtTestData.ExpiredMark(Cis(1), "water", DateTime.UtcNow.AddDays(-1));
        var package = GisMtTestData.ExpiredMark(Cis(2), "water", DateTime.UtcNow.AddDays(-1));

        var repository = new FakeJournalRepository();
        var weights = new Dictionary<string, int> { [unit.Cis] = 500, [package.Cis] = 500 };
        var packageTypes = new Dictionary<string, string> { [unit.Cis] = "UNIT", [package.Cis] = "GROUP" };
        var aggregation = new Dictionary<string, List<string>> { [package.Cis] = [unit.Cis] };

        var service = GisMtTestData.ReceiptService(
            repository,
            weights,
            null,
            new FakeMarksRepository(unit, package),
            packageTypes,
            aggregation);

        var filled = await service.FillExpired("2464122182", "water");

        Assert.True(filled.IsSuccess);
        var mark = Assert.Single(filled.Value.Marks);
        Assert.Equal(unit.Cis, mark.Cis);
        Assert.Equal(1, filled.Value.ExcludedPackages);
    }

    [Fact]
    public async Task Подбор_оставляет_упаковку_если_её_вложений_в_подборе_нет()
    {
        var unit = GisMtTestData.ExpiredMark(Cis(1), "water", DateTime.UtcNow.AddDays(-1));
        var package = GisMtTestData.ExpiredMark(Cis(2), "water", DateTime.UtcNow.AddDays(-1));
        var otherUnit = GisMtTestData.ExpiredMark(Cis(3), "water", DateTime.UtcNow.AddDays(-1));

        var repository = new FakeJournalRepository();
        var weights = new Dictionary<string, int> { [unit.Cis] = 500, [package.Cis] = 500 };
        var packageTypes = new Dictionary<string, string> { [unit.Cis] = "UNIT", [package.Cis] = "GROUP" };
        var aggregation = new Dictionary<string, List<string>> { [package.Cis] = [otherUnit.Cis] };

        var service = GisMtTestData.ReceiptService(
            repository,
            weights,
            null,
            new FakeMarksRepository(unit, package),
            packageTypes,
            aggregation);

        var filled = await service.FillExpired("2464122182", "water");

        Assert.True(filled.IsSuccess);
        Assert.Equal(2, filled.Value.Marks.Count);
        Assert.Equal(0, filled.Value.ExcludedPackages);
    }

    [Fact]
    public async Task При_сбое_агрегации_упаковки_остаются_в_подборе()
    {
        var unit = GisMtTestData.ExpiredMark(Cis(1), "water", DateTime.UtcNow.AddDays(-1));
        var package = GisMtTestData.ExpiredMark(Cis(2), "water", DateTime.UtcNow.AddDays(-1));

        var repository = new FakeJournalRepository();
        var weights = new Dictionary<string, int> { [unit.Cis] = 500, [package.Cis] = 500 };
        var packageTypes = new Dictionary<string, string> { [unit.Cis] = "UNIT", [package.Cis] = "GROUP" };

        var service = GisMtTestData.ReceiptService(
            repository,
            weights,
            null,
            new FakeMarksRepository(unit, package),
            packageTypes,
            aggregationFailure: "cises/aggregated/list HTTP 500");

        var filled = await service.FillExpired("2464122182", "water");

        Assert.True(filled.IsSuccess);
        Assert.Equal(2, filled.Value.Marks.Count);
        Assert.Equal(0, filled.Value.ExcludedPackages);
    }

    [Fact]
    public async Task Правка_не_затирает_уже_отправленный_акт()
    {
        var repository = new FakeJournalRepository();
        var service = GisMtTestData.ReceiptService(repository, [], null);

        var created = await service.Create("2464122182", [MarkWithWeight]);
        var documentId = created.Value.Document.Id;
        var reads = 0;

        repository.GetHook = document =>
        {
            reads++;

            if (reads == 1 || document is null)
                return document;

            document.Status = GisMtJournalStatuses.Sent;
            document.CrptDocumentId = "crpt-1";

            return document;
        };

        var updated = await service.Update(documentId, "2464122182", [MarkWithWeight]);

        Assert.True(updated.IsFailure);
        Assert.Contains("отправлен", updated.Error);
        Assert.Equal(GisMtJournalStatuses.Sent, repository.Saved.Single().Status);
        Assert.Equal("crpt-1", repository.Saved.Single().CrptDocumentId);
    }

    [Fact]
    public async Task Подбор_не_считает_отправленным_отклонённый_акт()
    {
        var returned = GisMtTestData.ExpiredMark(Cis(1), "water", DateTime.UtcNow.AddDays(-1));
        var repository = new FakeJournalRepository();

        await repository.Save(new FmuApiDomain.GisMt.Entities.GisMtJournalEntity
        {
            Id = "rejected-1",
            Kind = GisMtJournalKinds.LkReceipt,
            Reason = GisMtWriteOffReasons.Expiration,
            ProductGroup = "water",
            Status = GisMtJournalStatuses.Sent,
            CrptStatus = GisMtCrptStatuses.CheckedNotOk,
            Inn = "2464122182",
            CreatedAt = DateTime.Now,
            SentAt = DateTime.Now,
            Lines = [new FmuApiDomain.GisMt.Entities.GisMtJournalLine { Cis = returned.Cis }]
        });

        var service = GisMtTestData.ReceiptService(repository, [], null, new FakeMarksRepository(returned));

        var filled = await service.FillExpired("2464122182", "water");

        Assert.True(filled.IsSuccess);
        var mark = Assert.Single(filled.Value.Marks);
        Assert.Equal(returned.Cis, mark.Cis);
    }

    private static string Cis(int serial) => $"010460333400350921{serial:D6}";
}
