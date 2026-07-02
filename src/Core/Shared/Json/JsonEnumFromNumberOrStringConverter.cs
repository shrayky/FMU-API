using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Json;

public class JsonEnumFromNumberOrStringConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => (T)Enum.ToObject(typeof(T), reader.GetInt32()),
            JsonTokenType.String => ParseFromString(reader.GetString()),
            _ => throw new JsonException($"Неожиданный тип токена для {typeof(T).Name}: {reader.TokenType}")
        };
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(Convert.ToInt32(value));
    }

    private static T ParseFromString(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new JsonException("Пустое значение enum");

        if (int.TryParse(raw, out var number))
            return (T)Enum.ToObject(typeof(T), number);

        if (Enum.TryParse<T>(raw, ignoreCase: true, out var parsed))
            return parsed;

        throw new JsonException($"Не удалось преобразовать '{raw}' в {typeof(T).Name}");
    }
}
