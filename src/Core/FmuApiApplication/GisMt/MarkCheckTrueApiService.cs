using System.Globalization;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.GisMt;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Models;
using FmuApiDomain.State.Interfaces;
using FmuApiDomain.TrueApi.ProductInfo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.GisMt;

/// <summary>
/// Проверка марки через True API: cises/info, карточка товара, история кода и свой чек.
/// </summary>
[AutoRegisterService(ServiceLifetime.Scoped)]
public class MarkCheckTrueApiService(
    ILogger<MarkCheckTrueApiService> logger,
    IParametersService parametersService,
    IApplicationState applicationState,
    IGisMtCisesClient cisesClient,
    IGisMtCisHistoryClient historyClient,
    IGisMtReceiptClient receiptClient) : IMarkCheckTrueApiService
{
    private readonly ILogger<MarkCheckTrueApiService> _logger = logger;
    private readonly IParametersService _parametersService = parametersService;
    private readonly IApplicationState _applicationState = applicationState;
    private readonly IGisMtCisesClient _cisesClient = cisesClient;
    private readonly IGisMtCisHistoryClient _historyClient = historyClient;
    private readonly IGisMtReceiptClient _receiptClient = receiptClient;

    public async Task<MarkCheckTrueApiResult> CisesInfo(
        string inn,
        IReadOnlyList<string> cises,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = await _parametersService.CurrentAsync();
            var organisation = GisMtOrganisationResolver.Find(parameters, inn);

            if (organisation is null)
            {
                return MarkCheckTrueApiResult.WithStatus(
                    MarkCheckTrueApiStatuses.Disabled,
                    $"Организация с ИНН {inn} не найдена в настройках");
            }

            if (!organisation.TrueApiIntegrationSettings.Enable)
            {
                return MarkCheckTrueApiResult.WithStatus(
                    MarkCheckTrueApiStatuses.Disabled,
                    $"True API не подключён для ИНН {organisation.INN}");
            }

            if (!_applicationState.IsOnline())
            {
                return MarkCheckTrueApiResult.WithStatus(
                    MarkCheckTrueApiStatuses.Offline,
                    "Нет доступа к интернету");
            }

            var token = _applicationState.TrueApiToken(organisation.INN).Token;
            if (string.IsNullOrWhiteSpace(token))
            {
                return MarkCheckTrueApiResult.WithStatus(
                    MarkCheckTrueApiStatuses.NoToken,
                    $"Токен True API не получен для ИНН {organisation.INN}");
            }

            var cisList = cises
                .Select(GisMtCisNormalizer.ToCis)
                .Where(x => x.Length > 0)
                .ToList();

            var cisInfo = await _cisesClient.CisesInfo(
                token,
                cisList,
                productGroup: null,
                cancellationToken);

            if (cisInfo.IsFailure)
            {
                return MarkCheckTrueApiResult.WithStatus(
                    MarkCheckTrueApiStatuses.Error,
                    cisInfo.Error);
            }

            await EnrichWithProductInfo(token, cisInfo.Value, cancellationToken);

            var movements = await LoadMovements(token, cisList, cancellationToken);
            var result = MarkCheckTrueApiResult.Success(cisInfo.Value);
            result.Movements = movements.Items.Count == 0 ? null : movements.Items;
            result.HistoryReason = movements.Reason;
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка запроса cises/info для проверки марки, ИНН {Inn}", inn);
            return MarkCheckTrueApiResult.WithStatus(
                MarkCheckTrueApiStatuses.Error,
                ex.Message);
        }
    }

    /// <summary>
    /// История кода и свои чеки. Чужой документ и сбой истории не отменяют карточку марки.
    /// </summary>
    private async Task<MovementLoad> LoadMovements(
        string token,
        IReadOnlyList<string> cises,
        CancellationToken cancellationToken)
    {
        var items = new List<MarkCheckMovement>();
        var reasons = new List<string>();
        var receipts = new Dictionary<string, MarkCheckReceipt?>(StringComparer.OrdinalIgnoreCase);

        foreach (var cis in cises)
        {
            var history = await _historyClient.History(token, cis, cancellationToken);
            if (history.IsFailure)
            {
                reasons.Add(history.Error);
                continue;
            }

            foreach (var record in history.Value)
            {
                var movement = new MarkCheckMovement
                {
                    Cis = string.IsNullOrWhiteSpace(record.Cis) ? cis : record.Cis,
                    OperationDate = record.OperationDate,
                    DocId = record.DocId,
                    OwnerInn = record.OwnerInn,
                    OwnerName = record.OwnerName,
                    Status = record.Status
                };

                if (!string.IsNullOrWhiteSpace(record.DocId))
                    movement.Receipt = await OpenReceipt(token, record.DocId, receipts, cancellationToken);

                items.Add(movement);
            }
        }

        var reason = reasons.Count == 0 ? null : string.Join("; ", reasons.Distinct());
        return new MovementLoad(items, reason);
    }

    private async Task<MarkCheckReceipt?> OpenReceipt(
        string token,
        string docId,
        Dictionary<string, MarkCheckReceipt?> receipts,
        CancellationToken cancellationToken)
    {
        if (receipts.TryGetValue(docId, out var cached))
            return cached;

        var receipt = await _receiptClient.ReceiptInfo(token, docId, cancellationToken);
        var value = receipt.IsSuccess && receipt.Value.HasDetails
            ? ToReceipt(receipt.Value)
            : null;

        receipts[docId] = value;
        return value;
    }

    private static MarkCheckReceipt ToReceipt(GisMtReceiptInfo receipt) => new()
    {
        RetailAddress = receipt.RetailAddress,
        SoldAt = receipt.SoldAt?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
        UserInn = receipt.UserInn,
        KktRegId = receipt.KktRegId
    };

    private sealed record MovementLoad(List<MarkCheckMovement> Items, string? Reason);

    /// <summary>
    /// Дополняет сведения о КИ карточкой товара из product/info.
    /// </summary>
    private async Task EnrichWithProductInfo(
        string token,
        List<CisInfoResponseItem> items,
        CancellationToken cancellationToken)
    {
        var gtins = items
            .Select(item => item.CisInfo?.Gtin)
            .Where(gtin => !string.IsNullOrWhiteSpace(gtin))
            .Select(gtin => gtin!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (gtins.Count == 0)
            return;

        var products = await _cisesClient.ProductInfo(token, gtins, cancellationToken);
        if (products.IsFailure)
        {
            _logger.LogWarning("Не удалось получить product/info для проверки марки: {Error}", products.Error);
            return;
        }

        ApplyProductInfo(items, products.Value);
    }

    private static void ApplyProductInfo(List<CisInfoResponseItem> items, ProductsInformationTrueApi products)
    {
        if (products.Items.Count == 0)
            return;

        var byGtin = products.Items
            .Where(product => !string.IsNullOrWhiteSpace(product.Gtin))
            .GroupBy(product => product.Gtin, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            var info = item.CisInfo;
            if (info?.Gtin is null || !byGtin.TryGetValue(info.Gtin, out var product))
                continue;

            if (string.IsNullOrWhiteSpace(info.ProductName))
                info.ProductName = product.Name;

            if (string.IsNullOrWhiteSpace(info.Brand))
                info.Brand = product.Brand;

            if (string.IsNullOrWhiteSpace(info.ProducerName))
                info.ProducerName = product.ProducerName;

            if (string.IsNullOrWhiteSpace(info.ProducerInn))
                info.ProducerInn = product.Inn;

            if (string.IsNullOrWhiteSpace(info.TnVedEaes))
                info.TnVedEaes = product.TnVedEaes;

            if (info.ProductWeight is null)
                info.ProductWeight = product.ProductWeight;

            if (string.IsNullOrWhiteSpace(info.VolumeWeight))
                info.VolumeWeight = product.VolumeWeight;
        }
    }
}
