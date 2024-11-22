using System.Text.Json;

namespace FmuApiApplication.Utilites
{
    public static class JsonHelper
    {
        private static JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true};
        async public static ValueTask<T?> DeserializeAsync<T>(byte[] jData)
        {
            MemoryStream? stream = new(jData);

            return await DeserializeAsync<T>(stream);

        }
        async public static ValueTask<T?> DeserializeAsync<T>(MemoryStream stream)
        {
            var data = await JsonSerializer.DeserializeAsync<T>(stream, jsonSerializerOptions);

            return data;
        }

        async public static ValueTask<T?> DeserializeAsync<T>(Stream stream)
        {
            var data = await JsonSerializer.DeserializeAsync<T>(stream, jsonSerializerOptions);

            return data;
        }

    }
}

