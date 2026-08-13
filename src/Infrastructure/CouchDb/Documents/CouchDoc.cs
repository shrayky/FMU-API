using System.Text.Json.Serialization;

namespace CouchDb.Documents
{
    /// <summary>
    /// Обёртка документа CouchDB: доменная сущность в поле data + метаданные Id/Rev.
    /// </summary>
    public class CouchDoc<T> where T : class
    {
        public string Id { get; set; } = string.Empty;

        public string Rev { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public required T Data { get; set; }

        /// <summary>
        /// Создаёт документ из доменной сущности.
        /// </summary>
        public static CouchDoc<T> FromDomain(T entity, string? id)
        {
            return new CouchDoc<T>
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Data = entity
            };
        }

        /// <summary>
        /// Возвращает доменную сущность из документа.
        /// </summary>
        public T ToDomain()
        {
            return Data;
        }
    }
}
