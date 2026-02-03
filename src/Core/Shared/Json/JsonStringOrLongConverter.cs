using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Json;

public class JsonStringOrLongConverter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetInt64(),
            JsonTokenType.String => long.TryParse(reader.GetString(), out var result) 
                ? result 
                : throw new JsonException($"Не удалось преобразовать строку '{reader.GetString()}' в long"),
            _ => throw new JsonException($"Неожиданный тип токена: {reader.TokenType}")
        };
    }

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}