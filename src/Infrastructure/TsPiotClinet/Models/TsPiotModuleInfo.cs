using System.Text.Json.Serialization;

namespace TsPiotClinet.Models;

public record TsPiotModuleInfo
{
	[JsonPropertyName("appPath")]
	public string AppPath { get; set; } = string.Empty;

	[JsonPropertyName("logPath")]
	public string LogPath { get; set; } = string.Empty;

	[JsonPropertyName("version")]
	public string Version { get; set; } = string.Empty;
}