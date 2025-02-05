using System.Text.Json.Serialization;

namespace FmuApiDomain.TrueApi.ProductInfo
{
    public class ProductsInformationTrueApi
    {
        [JsonPropertyName("results")]
        public List<ProductInfoTrueApi> Results { get; set; } = [];
        [JsonPropertyName("total")]
        public int Total { get; set; } = 0;
        [JsonPropertyName("errorCode")]
        public string ErrorCode { get; set; } = string.Empty;

    }
}
