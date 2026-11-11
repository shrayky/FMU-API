using System.Text.Json;
using System.Text.Json.Serialization;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Journal;

namespace FmuApiApplication.GisMt.Journal;

/// <summary>
/// Тело документа LK_RECEIPT (списание весовой молочной продукции) для lk/documents/create.
/// </summary>
public static class GisMtLkReceiptBody
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string PrimaryDocumentType = "OTHER";
    private const string ExpirationDocumentName = "Акт списания марок по истечении срока годности";
    private const int DocumentIdLength = 8;

    public static string Build(GisMtJournalEntity document, GisMtParticipant participant)
    {
        var products = document.Lines
            .Select(line => new ReceiptProduct
            {
                Cis = line.Cis,
                ProductCost = line.AmountKop
            })
            .ToList();

        var action = GisMtWriteOffReasons.Resolve(document.Reason);
        var expiration = GisMtWriteOffReasons.IsExpiration(action);
        var actionDate = document.DocDate.ToString(DateFormat);

        var body = new ReceiptDocument
        {
            Inn = participant.Inn,
            Action = action,
            ActionDate = actionDate,
            Kpp = participant.IsIndividual || participant.Kpp.Length == 0 ? null : participant.Kpp,
            FiasId = !participant.IsIndividual || participant.FiasId.Length == 0 ? null : participant.FiasId,
            DocumentType = expiration ? PrimaryDocumentType : null,
            DocumentNumber = expiration ? BuildPrimaryDocumentNumber(document, actionDate) : null,
            DocumentDate = expiration ? actionDate : null,
            PrimaryDocumentCustomName = expiration ? ExpirationDocumentName : null,
            Products = products
        };

        return JsonSerializer.Serialize(body);
    }

    private static string BuildPrimaryDocumentNumber(GisMtJournalEntity document, string actionDate)
    {
        var id = (document.Id ?? string.Empty).Trim();
        var suffix = id.Length > DocumentIdLength ? id[..DocumentIdLength] : id;

        return suffix.Length == 0 ? actionDate : $"{actionDate}-{suffix}";
    }

    private sealed class ReceiptDocument
    {
        [JsonPropertyName("inn")]
        public string Inn { get; init; } = string.Empty;

        [JsonPropertyName("action")]
        public string Action { get; init; } = string.Empty;

        [JsonPropertyName("action_date")]
        public string ActionDate { get; init; } = string.Empty;

        [JsonPropertyName("kpp")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Kpp { get; init; }

        [JsonPropertyName("fias_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? FiasId { get; init; }

        [JsonPropertyName("document_type")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DocumentType { get; init; }

        [JsonPropertyName("document_number")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DocumentNumber { get; init; }

        [JsonPropertyName("document_date")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DocumentDate { get; init; }

        [JsonPropertyName("primary_document_custom_name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PrimaryDocumentCustomName { get; init; }

        [JsonPropertyName("products")]
        public List<ReceiptProduct> Products { get; init; } = [];
    }

    private sealed class ReceiptProduct
    {
        [JsonPropertyName("cis")]
        public string Cis { get; init; } = string.Empty;

        [JsonPropertyName("product_cost")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? ProductCost { get; init; }
    }
}
