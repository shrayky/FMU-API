using System.Text.Json.Serialization;

namespace TsPiotClinet.Models;

public record TsPiotCheckMarkRequestV3
{
    [JsonPropertyName("codes")]
    public List<CisObject> Codes { get; set; } = [];
    [JsonPropertyName("client_info")]
    public PmsrClientInfo ClientInfo { get; set; } = new();
}

public record CisObject
{
    [JsonPropertyName("cis")]
    public string Cis { get; set; } = string.Empty;

    [JsonPropertyName("pg")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Group { get; set; } = 0;

    public CisObject(string markBase64, int pg)
    {
        Cis = markBase64;
        Group = pg;
    }

    public CisObject(string markBase64)
    {
        Cis = markBase64;
    }

}