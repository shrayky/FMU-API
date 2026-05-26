using System.Text.Json.Serialization;

namespace TsPiotClinet.Models;

public record TsPiotKktInfo
{
	[JsonPropertyName("tspiotId")]
	public string Id { get; set; } = string.Empty;

	[JsonPropertyName("kktSerial")]
	public string KktSerialNumber { get; set; } = string.Empty;

	[JsonPropertyName("fnSerial")]
	public string FnSerialNumber { get; set; } = string.Empty;

	[JsonPropertyName("kktInn")]
	public string Inn { get; set; } = string.Empty;

	[JsonPropertyName("codesCheckTimeout")]
	public int CodesCheckTimeout { get; set; } = 0;

	[JsonPropertyName("lm")]
	public LocalModule LocalModeStatus { get; set; } = new();
}

public record LocalModule
{
	[JsonPropertyName("version")]
	public string Version { get; set; } = string.Empty;

	[JsonPropertyName("status")]
	public string Status { get; set; } = string.Empty;

    [JsonPropertyName("lastSync")]
	public long LastSyncTime { get; set; } = 0;

	[JsonPropertyName("token")]
	public string Token { get; set; } = string.Empty;

    [JsonPropertyName("expDate")]
	public string TokenExpirationDate {  get; set; } = string.Empty; // Дата и время истечения срока действия токена в формате ISO 8601 (например, 2025-03-22T10:30:00Z). Время указано в UTC.

    [JsonPropertyName("ip")]
	public string Ip {  get; set; } = string.Empty;

	[JsonPropertyName("port")]
	public int Port { get; set; } = 0;
}