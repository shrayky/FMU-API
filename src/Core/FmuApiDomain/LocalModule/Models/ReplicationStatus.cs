using System.Text.Json.Serialization;

namespace FmuApiDomain.LocalModule.Models
{
    public class ReplicationStatus
    {
        [JsonPropertyName("cis")]
        public DatabaseReplicationState? Cis { get; set; }

        [JsonPropertyName("blocked_series")]
        public DatabaseReplicationState? BlockedSeries { get; set; }

        [JsonPropertyName("blocked_gtin")]
        public DatabaseReplicationState? BlockedGtin { get; set; }

        [JsonPropertyName("blocked_cis")]
        public DatabaseReplicationState? BlockedCis { get; set; }
    }
}
