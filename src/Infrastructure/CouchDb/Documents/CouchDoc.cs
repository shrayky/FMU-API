using CouchDB.Driver.Types;
using Newtonsoft.Json;

namespace CouchDb.Documents
{
    public class CouchDoc<T> : CouchDocument where T : class
    {
        [JsonProperty("data")]
        public required T Data { get; set; }
        public static CouchDoc<T> FromDomain(T entity, string? id)
        {
            return new CouchDoc<T>
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Data = entity
            };
        }

        public T ToDomain()
        {
            return Data;
        }
    }
}
