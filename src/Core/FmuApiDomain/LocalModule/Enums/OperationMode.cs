using System.Text.Json.Serialization;

namespace FmuApiDomain.LocalModule.Enums
{
    public enum OperationMode
    {
        Unknown,
        [JsonPropertyName("active")]
        Active,

        [JsonPropertyName("service")]
        Service
    }
}
