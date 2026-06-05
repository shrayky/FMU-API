using System.Text.Json.Serialization;

namespace FmuApiDomain.DTO.FmuApiExchangeData.NodeInformation;

public record Payload
{
    [JsonPropertyName("node")]
    public NodeInformation NodeInformation { get; init; } = new();

    [JsonPropertyName("configuration")]
    public FmuApiSetting FmuApiSetting { get; init; } = new();

    [JsonPropertyName("cdns")]
    public List<CdnInformation> CdnInformation { get; init; } = [];

    [JsonPropertyName("localModules")]
    public List<LocalModuleInformation> LocalModuleInformation { get; init; } = [];

    [JsonPropertyName("tsPiots")]
    public List<TsPiotInformation> TsPiotsInforamtion {  get; init; } = [];

    [JsonPropertyName("statistics")]
    public List<CheckMarkStatisticInformation> CheckMarkStatisticInformation { get; init; } = [];
}