using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonSerializerOptionsProvider
{
    public static class JsonSerializerOptionsProvider
    {
        public static JsonSerializerOptions Default()
        {
            JsonSerializerOptions jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
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
