using System.Globalization;
using System.Text.Json;

namespace FmuApiDomain.GisMt.Models;

/// <summary>
/// Чек ККТ из ответа True API receipt/{id}/info.
/// </summary>
public class GisMtReceiptInfo
{
    public string? DocumentId { get; set; }

    public string? RetailAddress { get; set; }

    public DateTime? SoldAt { get; set; }

    public string? UserInn { get; set; }

    public string? KktRegId { get; set; }

    public bool HasDetails =>
        !string.IsNullOrWhiteSpace(RetailAddress)
        || SoldAt is not null
        || !string.IsNullOrWhiteSpace(UserInn)
        || !string.IsNullOrWhiteSpace(KktRegId);

    public static GisMtReceiptInfo FromElement(JsonElement element)
    {
        var receipt = ReceiptBody(element);
        var address = Text(receipt, "retailAddress");
        if (string.IsNullOrWhiteSpace(address) || address.Equals("Not Found", StringComparison.OrdinalIgnoreCase))
            address = Text(receipt, "retailPlace");

        return new GisMtReceiptInfo
        {
            DocumentId = Text(element, "documentId"),
            RetailAddress = string.IsNullOrWhiteSpace(address) ? null : address,
            SoldAt = Moment(receipt, "dateTime") ?? Moment(element, "docDate"),
            UserInn = Text(receipt, "userInn") ?? Text(element, "senderInn"),
            KktRegId = Text(receipt, "kktRegId")
        };
    }

    private static JsonElement ReceiptBody(JsonElement element)
    {
        if (!element.TryGetProperty("body", out var body))
            return element;

        if (body.ValueKind == JsonValueKind.String)
        {
            var json = body.GetString();
            if (string.IsNullOrWhiteSpace(json))
                return element;

            using var document = JsonDocument.Parse(json);
            body = document.RootElement.Clone();
        }

        if (body.ValueKind != JsonValueKind.Object)
            return element;

        if (body.TryGetProperty("receipt", out var receipt) && receipt.ValueKind == JsonValueKind.Object)
            return receipt;

        if (body.TryGetProperty("bso", out var bso) && bso.ValueKind == JsonValueKind.Object)
            return bso;

        return body;
    }

    private static string? Text(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
            return null;

        var text = value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private static DateTime? Moment(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
            return null;

        switch (value.ValueKind)
        {
            case JsonValueKind.Number when value.TryGetInt64(out var unix):
                return FromUnix(unix);
            case JsonValueKind.Number when value.TryGetDouble(out var unixDouble):
                return FromUnix((long)unixDouble);
            case JsonValueKind.String:
                var text = value.GetString();
                if (string.IsNullOrWhiteSpace(text))
                    return null;

                if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unixText))
                    return FromUnix(unixText);

                if (DateTime.TryParse(
                        text,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                        out var date))
                    return DateTime.SpecifyKind(date.ToUniversalTime(), DateTimeKind.Utc);

                return null;
            default:
                return null;
        }
    }

    private static DateTime FromUnix(long unix)
    {
        var moment = unix > 10_000_000_000
            ? DateTimeOffset.FromUnixTimeMilliseconds(unix)
            : DateTimeOffset.FromUnixTimeSeconds(unix);

        return moment.UtcDateTime;
    }
}
