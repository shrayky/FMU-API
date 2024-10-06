using System.Text.Json.Serialization;

namespace FmuApiDomain.Models.TrueSignApi.MarkData
{
    public class CodeDataTrueApi
    {
        [JsonPropertyName("cis")]
        public string Cis { get; set; } = string.Empty;
        [JsonPropertyName("valid")]
        public bool Valid { get; set; } = false;
        [JsonPropertyName("printView")]
        public string PrintView { get; set; } = string.Empty;
        [JsonPropertyName("gtin")]
        public string Gtin { get; set; } = string.Empty;
        [JsonPropertyName("groupIds")]
        public List<int> GroupIds { get; set; } = new();
        [JsonPropertyName("verified")]
        public bool Verified { get; set; } = false;
        [JsonPropertyName("message")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Message { get; set; } = string.Empty;
        [JsonPropertyName("found")]
        public bool Found { get; set; } = false;
        [JsonPropertyName("utilised")]
        public bool Utilised { get; set; } = false;
        [JsonPropertyName("isOwner")]
        public bool IsOwner { get; set; } = false;
        [JsonPropertyName("isBlocked")]
        public bool IsBlocked { get; set; } = false;
        [JsonPropertyName("productionDate")]
        public DateTime ProductionDate { get; set; } = DateTime.MinValue;
        [JsonPropertyName("prVetDocument")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PrVetDocument { get; set; }
        [JsonPropertyName("errorCode")]
        public int ErrorCode { get; set; } = 0;
        [JsonPropertyName("isTracking")]
        public bool IsTracking { get; set; } = false;
        [JsonPropertyName("sold")]
        public bool Sold { get; set; } = false;
        [JsonPropertyName("realizable")]
        public bool Realizable { get; set; } = false;
        [JsonPropertyName("packageType")]
        public string PackageType { get; set; } = string.Empty;
        [JsonPropertyName("producerInn")]
        public string ProducerInn { get; set; } = string.Empty;
        [JsonPropertyName("grayZone")]
        public bool GrayZone { get; set; } = false;
        [JsonPropertyName("mrp")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Mrp { get; set; }
        [JsonPropertyName("smp")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Smp { get; set; }
        [JsonPropertyName("expireDate")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? ExpireDate { get; set; }
        [JsonPropertyName("innerUnitCount")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? InnerUnitCount { get; set; }
        [JsonPropertyName("soldUnitCount")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? SoldUnitCount {  get; set; }
        [JsonIgnore]
        public bool IsExpired { get => ExpireDate < DateTime.Now; }
        [JsonIgnore]
        public bool CodeFounded { get => ErrorCode != 10; }
        [JsonIgnore]
        public int DaysExpired { get => ExpireDate is null ? 999 : Convert.ToInt32((DateTime.Now - ExpireDate).Value.TotalDays); }
        [JsonIgnore]
        public bool Empty { get => Cis == string.Empty; }

        public bool InGroup(string ignoreVerificationErrorForTrueApiGroups)
        {
            var ignoredCodes = ignoreVerificationErrorForTrueApiGroups.Split(" ");

            foreach (int groupCode in GroupIds)
            {
                foreach (string ignoredCode in ignoredCodes)
                {
                    if (ignoredCode == groupCode.ToString())
                        return true;
                }
            }

            return false;
        }

        public string MarkErrorDescription()
        {
            if (Sold)
                return "Товар с этой маркой уже продан!";

            if (!CodeFounded)
                return "Код марки не найден в базе честного знака!";

            if (IsBlocked)
                return "Эта марка заблокирована по решению государственных органов!";

            if (GrayZone)
                return "Марка находится в серой зоне - продажа невозможна!";

            if (IsExpired)
                return $"Срок годности вышел {DaysExpired} дней назад!";

            return "";
        }
    }
}
