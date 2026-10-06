using System.Text.Json.Serialization;

namespace CouchDb.DatabaseScheme;

/// <summary>
/// Описание индекса для создания через CouchDB API (POST /{db}/_index).
/// </summary>
public sealed record CouchDbIndexDefinition(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("index")] CouchDbIndexBody Index)
{
    [JsonPropertyName("ddoc")]
    public string Ddoc => Name;
}

/// <summary>
/// Тело индекса с перечнем полей для Mango-индекса.
/// Поле — строка (по возрастанию) или объект с направлением, например {"data.checkDate": "desc"}.
/// </summary>
public sealed record CouchDbIndexBody(
    [property: JsonPropertyName("fields")] object[] Fields);

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

    [JsonPropertyName("ddoc")]
    public string Ddoc { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}
