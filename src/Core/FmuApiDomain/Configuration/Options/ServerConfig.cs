namespace FmuApiDomain.Configuration.Options;

using System.Text.Json.Serialization;
using Shared.Json;

public class ServerConfig
{
    public int ApiIpPort { get; set; } = 2578;
    public bool TsPiotEnabled { get; set; } = true;
    public int LocalModuleVersion { get; set; } = 2;

    [JsonConverter(typeof(JsonEnumFromNumberOrStringConverter<DocumentResponseEncoding>))]
    public DocumentResponseEncoding ResponseEncoding { get; set; } = DocumentResponseEncoding.Utf8;
}
