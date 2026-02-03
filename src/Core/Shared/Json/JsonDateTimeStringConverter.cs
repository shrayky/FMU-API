using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Json;

public class JsonDateTimeStringConverter : JsonConverter<DateTime>
{
    private static readonly string[] SupportedFormats = 
    {
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.fffZ"
    };

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var dateString = reader.GetString();
            if (string.IsNullOrEmpty(dateString))
                return DateTime.MinValue;

            foreach (var format in SupportedFormats)
            {
                if (DateTime.TryParseExact(dateString, format, null, System.Globalization.DateTimeStyles.None, out var result))
                    return result;
            }

            if (DateTime.TryParse(dateString, out var parsedDate))
                return parsedDate;

            throw new JsonException($"Не удалось преобразовать строку '{dateString}' в DateTime");
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt64(out var timestamp))
            {
                return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
            }
        }

        throw new JsonException($"Неожиданный тип токена для DateTime: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-dd HH:mm:ss"));
    }
}