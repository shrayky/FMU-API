using System.Text.Json.Serialization;

namespace FmuApiDomain.GisMt.Models;

public class CisSearchRequest
{
    [JsonPropertyName("filter")]
    public CisSearchFilter Filter { get; set; } = new();

    [JsonPropertyName("pagination")]
    public CisSearchPagination? Pagination { get; set; }
}

public class CisSearchFilter
{
    [JsonPropertyName("states")]
    public List<CisSearchState> States { get; set; } = [];

    [JsonPropertyName("productGroups")]
    public List<string> ProductGroups { get; set; } = [];
}

public class CisSearchState
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

public class CisSearchPagination
{
    [JsonPropertyName("perPage")]
    public int PerPage { get; set; } = 1000;

    [JsonPropertyName("lastEmissionDate")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LastEmissionDate { get; set; }

    [JsonPropertyName("sgtin")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Sgtin { get; set; }

    [JsonPropertyName("direction")]
    public int Direction { get; set; }
}

public class CisSearchResponse
{
    [JsonPropertyName("isLastPage")]
    public bool IsLastPage { get; set; }

    [JsonPropertyName("result")]
    public List<CisSearchResultItem> Result { get; set; } = [];
}

public class CisSearchResultItem
{
    [JsonPropertyName("cis")]
    public string? Cis { get; set; }

    [JsonPropertyName("gtin")]
    public string? Gtin { get; set; }

    [JsonPropertyName("sgtin")]
    public string? Sgtin { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("emissionDate")]
    public DateTime? EmissionDate { get; set; }

    [JsonPropertyName("productGroup")]
    public string? ProductGroup { get; set; }

    [JsonPropertyName("productGroupId")]
    public int ProductGroupId { get; set; }

    [JsonPropertyName("ownerInn")]
    public string? OwnerInn { get; set; }

    [JsonPropertyName("producerInn")]
    public string? ProducerInn { get; set; }
}
