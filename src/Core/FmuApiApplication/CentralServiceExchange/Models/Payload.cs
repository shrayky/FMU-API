using FmuApiApplication.StateCollectors.Models;
using FmuApiDomain.DTO.FmuApiExchangeData;
using System.Text.Json.Serialization;

namespace FmuApiApplication.CentralServiceExchange.Models;

public record Payload
{
    [JsonPropertyName("node")]
    public NodeInformation NodeInformation { get; init; } = new();

    [JsonPropertyName("configuration")]
    public FmuApiSetting FmuApiSetting { get; init; } = new();

    [JsonPropertyName("cdns")]
    public List<CdnInformation> CdnInformation { get; init; } = [];

    [JsonPropertyName("localModules")]
    public List<LocalModuleStateInformation> LocalModuleInformation { get; init; } = [];

    [JsonPropertyName("tsPiots")]
    public List<TsPiotStateInfotmation> TsPiotsInforamtion {  get; init; } = [];

    [JsonPropertyName("statistics")]
    public List<CheckMarkStatisticInformation> CheckMarkStatisticInformation { get; init; } = [];
}