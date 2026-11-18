using System.Text.Json;
using CSharpFunctionalExtensions;
using FmuApiApplication.GisMt;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Models;
using FmuApiDomain.TrueApi.ProductInfo;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FmuApiApplication.Tests;

public class MarkCheckTrueApiServiceTests
{
    [Fact]
    public async Task Проверка_марки_добавляет_историю_и_свой_чек()
    {
        var receipts = new FakeReceiptClient();
        var service = Service(
            new FakeHistoryClient(
            [
                Event("cis-1", "receipt-1", "2026-03-01T10:00:00.000Z"),
                Event("cis-1", "upd-1", "2026-02-01T10:00:00.000Z"),
                Event("cis-1", "receipt-1", "2026-03-01T10:00:00.000Z")
            ]),
            receipts);

        var result = await service.CisesInfo("2464122182", ["cis-1"]);

        Assert.Equal(MarkCheckTrueApiStatuses.Ok, result.Status);
        Assert.Null(result.HistoryReason);
        var info = result.Data?[0].CisInfo;
        Assert.NotNull(info);
        Assert.True(info.IsMultipleSales);
        Assert.True(info.MarkWithdraw);

        Assert.Equal(3, result.Movements?.Count);
        var receipt = result.Movements?[0].Receipt;
        Assert.NotNull(receipt);
        Assert.Equal("ул. Ленина, 1", receipt.RetailAddress);
        Assert.Equal("2020-12-20T21:43:00.000Z", receipt.SoldAt);
        Assert.Equal("2464122182", receipt.UserInn);
        Assert.Equal("0000000000001234", receipt.KktRegId);
        Assert.Null(result.Movements?[1].Receipt);
        Assert.Same(receipt, result.Movements?[2].Receipt);
        Assert.Equal(["receipt-1", "upd-1"], receipts.Calls);
    }

    [Fact]
    public async Task Сбой_истории_оставляет_карточку_марки()
    {
        var receipts = new FakeReceiptClient();
        var service = Service(new FakeHistoryClient(failure: "cises/history HTTP 404"), receipts);

        var result = await service.CisesInfo("2464122182", ["cis-1"]);

        Assert.Equal(MarkCheckTrueApiStatuses.Ok, result.Status);
        Assert.Equal("В обороте", result.Data![0].CisInfo!.Status);
        Assert.Equal("cises/history HTTP 404", result.HistoryReason);
        Assert.Null(result.Movements);
        Assert.Empty(receipts.Calls);
    }

    [Fact]
    public void Чек_берёт_время_из_unix_и_адрес_точки()
    {
        using var document = JsonDocument.Parse(
            """
            {
              "documentId": "receipt-1",
              "senderInn": "2464122182",
              "body": {
                "receipt": {
                  "dateTime": 1608500580,
                  "userInn": "2464122182",
                  "kktRegId": "0000000000001234",
                  "retailAddress": "Not Found",
                  "retailPlace": "ул. Ленина, 1"
                }
              }
            }
            """);

        var info = GisMtReceiptInfo.FromElement(document.RootElement);

        Assert.Equal("ул. Ленина, 1", info.RetailAddress);
        Assert.Equal(new DateTime(2020, 12, 20, 21, 43, 0, DateTimeKind.Utc), info.SoldAt);
        Assert.Equal("2464122182", info.UserInn);
        Assert.Equal("0000000000001234", info.KktRegId);
        Assert.True(info.HasDetails);
    }

    private static MarkCheckTrueApiService Service(IGisMtCisHistoryClient history, IGisMtReceiptClient receipts)
    {
        var parameters = GisMtTestData.Parameters();
        parameters.OrganisationConfig.PrintGroups[0].TrueApiIntegrationSettings.Enable = true;

        return new MarkCheckTrueApiService(
            NullLogger<MarkCheckTrueApiService>.Instance,
            new FakeParameters(parameters),
            GisMtTestData.TokenState(),
            new MarkCheckCisesClient(),
            history,
            receipts);
    }

    private static GisMtCisHistoryRecord Event(string cis, string docId, string operationDate) => new()
    {
        Cis = cis,
        DocId = docId,
        OperationDate = operationDate,
        OwnerInn = "2464122182",
        OwnerName = "Магазин",
        Status = "RETIRED"
    };

    private sealed class MarkCheckCisesClient : IGisMtCisesClient
    {
        public Task<Result<List<CisInfoResponseItem>>> CisesInfo(
            string token,
            IReadOnlyList<string> cises,
            string? productGroup,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Success(new List<CisInfoResponseItem>
            {
                new()
                {
                    CisInfo = new CisInfoData
                    {
                        Cis = cises[0],
                        Status = "В обороте",
                        IsMultipleSales = true,
                        MarkWithdraw = true
                    }
                }
            }));
        }

        public Task<Result<CisSearchResponse>> SearchCises(
            string token,
            CisSearchRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Result<ProductsInformationTrueApi>> ProductInfo(
            string token,
            List<string> gtins,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success(new ProductsInformationTrueApi()));
    }

    private sealed class FakeHistoryClient : IGisMtCisHistoryClient
    {
        private readonly IReadOnlyList<GisMtCisHistoryRecord> _records;
        private readonly string? _failure;

        public FakeHistoryClient(IReadOnlyList<GisMtCisHistoryRecord> records)
        {
            _records = records;
        }

        public FakeHistoryClient(string failure)
        {
            _records = [];
            _failure = failure;
        }

        public Task<Result<List<GisMtCisHistoryRecord>>> History(
            string token,
            string cis,
            CancellationToken cancellationToken = default)
        {
            if (_failure is not null)
                return Task.FromResult(Result.Failure<List<GisMtCisHistoryRecord>>(_failure));

            return Task.FromResult(Result.Success(_records.ToList()));
        }
    }

    private sealed class FakeReceiptClient : IGisMtReceiptClient
    {
        public List<string> Calls { get; } = [];

        public Task<Result<GisMtReceiptInfo>> ReceiptInfo(
            string token,
            string receiptId,
            CancellationToken cancellationToken = default)
        {
            Calls.Add(receiptId);

            if (receiptId != "receipt-1")
                return Task.FromResult(Result.Failure<GisMtReceiptInfo>("receipt/info HTTP 404"));

            return Task.FromResult(Result.Success(new GisMtReceiptInfo
            {
                DocumentId = receiptId,
                RetailAddress = "ул. Ленина, 1",
                SoldAt = new DateTime(2020, 12, 20, 21, 43, 0, DateTimeKind.Utc),
                UserInn = "2464122182",
                KktRegId = "0000000000001234"
            }));
        }
    }
}
