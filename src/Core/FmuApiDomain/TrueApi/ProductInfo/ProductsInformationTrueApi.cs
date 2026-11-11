using System.Text.Json.Serialization;

namespace FmuApiDomain.TrueApi.ProductInfo
{
    public class ProductsInformationTrueApi
    {
        [JsonPropertyName("results")]
        public List<ProductInfoTrueApi>? Results { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; } = 0;

        [JsonPropertyName("errorCode")]
        public string ErrorCode { get; set; } = string.Empty;

        /// <summary>
        /// Карточки товаров; пустой список, если ГИС МТ вернула null или не вернула поле.
        /// </summary>
        [JsonIgnore]
        public IReadOnlyList<ProductInfoTrueApi> Items => Results ?? [];
    }
}
