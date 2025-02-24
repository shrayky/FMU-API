using System.Text.Json.Serialization;

namespace FmuApiDomain.LocalModule.Enums
{
    public enum LocalModuleStatus
    {
        [JsonPropertyName("not_configured")]
        NotConfigured,

        [JsonPropertyName("initialization")]
        Initialization,

        [JsonPropertyName("ready")]
        Ready,

        [JsonPropertyName("sync_error")]
        SyncError,

        Unknown
    }
}

