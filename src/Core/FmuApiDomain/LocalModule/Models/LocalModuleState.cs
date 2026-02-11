using FmuApiDomain.LocalModule.Enums;
using System.Text.Json.Serialization;

namespace FmuApiDomain.LocalModule.Models
{
    public class LocalModuleState
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string StatusRaw { get; set; } = string.Empty;
        
        [JsonPropertyName("serviceUrl")]
        public string ServiceUrl { get; set; } = string.Empty;
        
        [JsonPropertyName("operationMode")]
        public string OperationModeRaw { get; set; } = string.Empty;
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("lastUpdate")]
        public long LastUpdate { get; set; }
        
        [JsonPropertyName("lastSync")]
        public long LastSyncTimestamp { get; set; }
        
        [JsonPropertyName("inst")]
        public string InstanceId { get; set; } = string.Empty;

        [JsonPropertyName("inn")]
        public string Inn { get; set; } = string.Empty;
        
        [JsonPropertyName("dbVersion")]
        public string DbVersion { get; set; } = string.Empty;

        [JsonPropertyName("requiresDownload")]
        public bool RequiresDownload { get; set; }

        public DateTime LastSyncDateTime =>
            DateTimeOffset.FromUnixTimeMilliseconds(LastSyncTimestamp).DateTime;

        [JsonIgnore]
        public LocalModuleStatus Status => StatusRaw?.ToLower() switch
        {
            "ready" => LocalModuleStatus.Ready,
            "initialization" => LocalModuleStatus.Initialization,
            "sync_error" => LocalModuleStatus.SyncError,
            "not_configured" => LocalModuleStatus.NotConfigured,
            "enisey_off-line" => LocalModuleStatus.EniseyOfflie,
            _ => LocalModuleStatus.Unknown
        };

        [JsonIgnore]
        private OperationMode OperationMode => OperationModeRaw?.ToLower() switch
        {
            "active" => OperationMode.Active,
            "service" => OperationMode.Service,
            _ => OperationMode.Unknown
        };
        
        [JsonIgnore]
        public bool IsConfigured => Status != LocalModuleStatus.NotConfigured;
        
        [JsonIgnore]
        public bool IsReady => Status == LocalModuleStatus.Ready;
        
        [JsonIgnore]
        public bool HasSyncError => Status == LocalModuleStatus.SyncError;
        
        [JsonIgnore]
        public bool IsInServiceMode => OperationMode == OperationMode.Service;
    }
}
