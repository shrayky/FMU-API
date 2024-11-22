using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonSerilizerOptions
{
    public static class GeneralJsonSerilizerOptions
    {
        public static JsonSerializerOptions Default()
        {
            JsonSerializerOptions jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                Converters =
                {
                    new JsonToBoolConverter()
                }
            };

            return jsonOptions;
        }
    }
}
