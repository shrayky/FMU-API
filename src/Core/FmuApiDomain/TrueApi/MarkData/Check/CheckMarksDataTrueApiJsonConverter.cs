using System.Text.Json;
using System.Text.Json.Serialization;

namespace FmuApiDomain.TrueApi.MarkData.Check;

/// <summary>
/// Пустой ответ Честного знака сериализует как {}, без нулей и пустых строк.
/// </summary>
public class CheckMarksDataTrueApiJsonConverter : JsonConverter<CheckMarksDataTrueApi>
{
    public override CheckMarksDataTrueApi Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<CheckMarksDataTrueApi>(ref reader, options) ?? new();
    }

    public override void Write(Utf8JsonWriter writer, CheckMarksDataTrueApi value, JsonSerializerOptions options)
    {
        if (IsEmpty(value))
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
            return;
        }

        JsonSerializer.Serialize(writer, value, options);
    }

    private static bool IsEmpty(CheckMarksDataTrueApi value)
    {
        return value.Code == 0
            && value.ReqTimestamp == 0
            && string.IsNullOrEmpty(value.Description)
            && string.IsNullOrEmpty(value.ReqId)
            && string.IsNullOrEmpty(value.Inst)
            && string.IsNullOrEmpty(value.Version)
            && value.Codes.Count == 0;
    }
}
