using System.Text.Json.Serialization;

namespace FmuApiDomain.Configuration.Options;

/// <summary>
/// Хранилище собственных данных приложения: CouchDB или файл SQLite.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DatabaseProvider
{
    CouchDb = 0,
    Sqlite = 1
}
