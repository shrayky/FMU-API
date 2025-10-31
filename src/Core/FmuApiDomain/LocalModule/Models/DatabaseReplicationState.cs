using System.Text.Json.Serialization;
using static Shared.Json.JsonSerializeOptionsProvider;

namespace FmuApiDomain.LocalModule.Models
{
    public class DatabaseReplicationState
    {
        [JsonPropertyName("localDocCount")]
        [JsonConverter(typeof(JsonStringOrIntConverter))]
        public string LocalDocCountRaw { get; set; } = string.Empty;

        [JsonPropertyName("serverDocCount")]
        [JsonConverter(typeof(JsonStringOrIntConverter))]
        public string ServerDocCountRaw { get; set; } = string.Empty;

        [JsonPropertyName("timeLag")]
        [JsonConverter(typeof(JsonStringOrIntConverter))]
        public string TimeLagRaw { get; set; } =  string.Empty;

        public int? DocCountLocal => ParseIntOrNull(LocalDocCountRaw);
        public int? DocCountServer => ParseIntOrNull(ServerDocCountRaw);
        public long? TimeLagMilliseconds => ParseLongOrNull(TimeLagRaw);

        public bool HasValidCounts =>
            DocCountLocal.HasValue && DocCountServer.HasValue;

        public bool IsSynced =>
            HasValidCounts && DocCountLocal == DocCountServer;

        public double? SyncProgress =>
            HasValidCounts && DocCountServer > 0 ?
            (DocCountLocal * 100.0 / DocCountServer) :
            null;

        public bool IsUnknown =>
            LocalDocCountRaw == "unknown" &&
            ServerDocCountRaw == "unknown" &&
            TimeLagRaw == "unknown";

        private static int? ParseIntOrNull(string value) =>
            value != "unknown" && int.TryParse(value, out int result) ?
            result :
            null;

        private static long? ParseLongOrNull(string value) =>
            value != "unknown" && long.TryParse(value, out long result) ?
            result :
            null;
    }

}

