using FmuApiDomain.LocalModule.Enums;
using System.Text.Json.Serialization;

namespace FmuApiDomain.LocalModule.Models
{
    public class LocalModuleState
    {
        [JsonPropertyName("lastSync")]
        public long LastSyncTimestamp { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("inst")]
        public string InstanceId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string StatusRaw { get; set; } = string.Empty;

        [JsonPropertyName("operationMode")]
        public string OperationModeRaw { get; set; } = string.Empty;

        [JsonPropertyName("requiresDownload")]
        public bool RequiresDownload { get; set; }

        [JsonPropertyName("replicationStatus")]
        public ReplicationStatus? ReplicationStatus { get; set; }

        public DateTime LastSyncDateTime =>
            DateTimeOffset.FromUnixTimeMilliseconds(LastSyncTimestamp).DateTime;

        [JsonIgnore]
        public LocalModuleStatus Status => StatusRaw?.ToLower() switch
        {
            "ready" => LocalModuleStatus.Ready,
            "initialization" => LocalModuleStatus.Initialization,
            "sync_error" => LocalModuleStatus.SyncError,
            "not_configured" => LocalModuleStatus.NotConfigured,
            _ => LocalModuleStatus.Unknown
        };

        [JsonIgnore]
        public OperationMode OperationMode => OperationModeRaw?.ToLower() switch
        {
            "active" => OperationMode.Active,
            "service" => OperationMode.Service,
            _ => OperationMode.Unknown
        };

        public bool IsConfigured => Status != LocalModuleStatus.NotConfigured;
        public bool IsReady => Status == LocalModuleStatus.Ready;
        public bool HasSyncError => Status == LocalModuleStatus.SyncError;
        public bool IsInServiceMode => OperationMode == OperationMode.Service;
    }
}
