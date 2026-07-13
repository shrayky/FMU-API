using System.Text.Json.Serialization;

namespace CouchDb.Models
{
    /// <summary>
    /// Описание индекса для создания через CouchDB API (POST /{db}/_index).
    /// </summary>
    public sealed record CouchDbIndexDefinition(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("index")] CouchDbIndexBody Index);

    /// <summary>
    /// Тело индекса с перечнем полей для Mango-индекса.
    /// </summary>
    public sealed record CouchDbIndexBody(
        [property: JsonPropertyName("fields")] string[] Fields);

    /// <summary>
    /// Ответ CouchDB на запрос списка индексов (GET /{db}/_index).
    /// </summary>
    public sealed class CouchDbIndexListResponse
    {
        [JsonPropertyName("indexes")]
        public List<CouchDbIndexEntry> Indexes { get; set; } = [];
    }

    /// <summary>
    /// Элемент списка индексов из ответа CouchDB.
    /// </summary>
    public sealed class CouchDbIndexEntry
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}
