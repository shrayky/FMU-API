namespace FmuApiDomain.Configuration.Options;

using Shared.Json;
using System.Text.Json.Serialization;

public class ServerConfig
{
    public int ApiIpPort { get; set; } = 2578;
    
    public bool TsPiotEnabled { get; set; } = true;

    public LocalModuleGeneral LocalModuleGeneral { get; set; } = new();

    [JsonConverter(typeof(JsonEnumFromNumberOrStringConverter<DocumentResponseEncoding>))]
    public DocumentResponseEncoding ResponseEncoding { get; set; } = DocumentResponseEncoding.Utf8;

    [Obsolete]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LocalModuleVersion { get; set; }
}
