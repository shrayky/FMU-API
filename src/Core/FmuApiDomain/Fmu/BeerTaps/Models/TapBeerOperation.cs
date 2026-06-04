using FmuApiDomain.Fmu.Document;
using System.Text.Json.Serialization;

namespace FmuApiDomain.Fmu.BeerTaps.Models;

public class TapBeerOperation
{
    [JsonPropertyName("uid")]
    public string Uid { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("pos")]
    public string Pos { get; set; } = string.Empty;

    [JsonPropertyName("shift")]
    public string Shift { get; set; } = string.Empty;

    [JsonPropertyName("number")]
    public string Number { get; set; } = string.Empty;

    [JsonPropertyName("user")]
    public string User { get; set; } = string.Empty;

    [JsonPropertyName("keg_whitelist")]
    public int KegWhiteListRegime { get; set; } = 0; // 0 - автоподстановка выключена, 1 - включена, 2 - белый список

    [JsonPropertyName("emptied_marking_code")]
    public string EmptiedMarkingCode { get; set; } = string.Empty; //  код маркировки опустошенного кега в формате base64

    [JsonPropertyName("position")]
    public Position Position { get; set; } = new();
}

public class Position
{
    [JsonPropertyName("marking_code")]
    public string MarkingCode { get; set; } = string.Empty; // код маркировки кега(с идентификаторамиприменения и криптохвостом) вформате base64.

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty; // Идентификатор товара в АСТУ

    [JsonPropertyName("item_type")]
    public string ItemType { get; set; } = string.Empty;

    [JsonPropertyName("Volume")]
    public int Volume { get; set; } = 0;

    [JsonPropertyName("Text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("expiration_period")]
    public string ExpirationPeriod { get; set; } = string.Empty;

    [JsonPropertyName("total_price")]
    public float TotalPrice { get; set; } = 0; // цена за 1 мл

    [JsonPropertyName("organisation")]
    public Organization Organisation { get; set; } = new();
}