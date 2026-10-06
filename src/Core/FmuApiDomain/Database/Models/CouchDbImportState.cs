using System.Text.Json.Serialization;

namespace FmuApiDomain.Database.Models;

/// <summary>
/// Фаза переноса данных из CouchDB в SQLite.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CouchDbImportPhase
{
    Idle,
    Running,
    Completed,
    Failed
}

/// <summary>
/// Состояние хода переноса данных из CouchDB в SQLite.
/// </summary>
public class CouchDbImportState
{
    public CouchDbImportPhase Phase { get; init; } = CouchDbImportPhase.Idle;
    public string CurrentDatabase { get; init; } = string.Empty;
    public int CopiedInCurrent { get; init; }
    public int CompletedDatabases { get; init; }
    public int TotalDatabases { get; init; } = 7;
    public string Error { get; init; } = string.Empty;
}
