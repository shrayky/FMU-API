using System.Text.Json.Serialization;

namespace FmuApiDomain.TrueApi.ProductInfo
{
    /// <summary>
    /// Карточка товара из True API product/info.
    /// </summary>
    public class ProductInfoTrueApi
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("gtin")]
        public string Gtin { get; set; } = string.Empty;

        [JsonPropertyName("packageType")]
        public string PackageType { get; set; } = string.Empty;

        [JsonPropertyName("innerUnitCount")]
        public int InnerUnitCount { get; set; } = 0;

        [JsonPropertyName("productGroupId")]
        public int ProductGroupId { get; set; } = 0;

        [JsonPropertyName("productGroup")]
        public string ProductGroup { get; set; } = string.Empty;

        [JsonPropertyName("brand")]
        public string? Brand { get; set; }

        [JsonPropertyName("inn")]
        public string? Inn { get; set; }

        [JsonPropertyName("producerName")]
        public string? ProducerName { get; set; }

        [JsonPropertyName("tnVedEaes")]
        public string? TnVedEaes { get; set; }

        [JsonPropertyName("productWeight")]
        public double? ProductWeight { get; set; }

        [JsonPropertyName("volumeWeight")]
        public string? VolumeWeight { get; set; }
    }
}
