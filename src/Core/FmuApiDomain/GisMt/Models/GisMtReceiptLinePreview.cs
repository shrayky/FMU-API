namespace FmuApiDomain.GisMt.Models;

/// <summary>
/// Строка списания до сохранения: КИ, название, количество, цена и сумма, посчитанные по марке.
/// </summary>
public record GisMtReceiptLinePreview(
    string Mark,
    string Cis,
    string Gtin,
    string ProductName,
    decimal? QuantityKg,
    decimal? PricePerKg,
    int? AmountKop,
    string Error);
