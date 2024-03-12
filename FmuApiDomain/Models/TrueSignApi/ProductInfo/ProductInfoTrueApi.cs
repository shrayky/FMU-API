namespace FmuApiDomain.Models.TrueSignApi.ProductInfo
{
    public class ProductInfoTrueApi
    {
        public string Name { get; set; } = string.Empty;
        public string Gtin { get; set; } = string.Empty;
        public string PackageType { get; set; } = string.Empty;
        public int InnerUnitCount { get; set; } = 0;
        public int ProductGroupId { get; set; } = 0;
        public string ProductGroup {  get; set; } = string.Empty;


    }
}
