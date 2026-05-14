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
}