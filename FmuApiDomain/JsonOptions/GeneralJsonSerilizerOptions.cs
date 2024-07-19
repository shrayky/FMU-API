using System.Text.Json;

namespace FmuApiDomain.JsonOptions
{
    public static class GeneralJsonSerilizerOptions
    {
        public static JsonSerializerOptions SerializerOptions()
        {
            JsonSerializerOptions jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                Converters =
                {
                    new JsonToBoolConverter()
                }
            };

            return jsonOptions;
        }
    }
}
