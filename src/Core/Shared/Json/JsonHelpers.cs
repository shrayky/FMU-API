using System.Text.Json;

namespace Shared.Json
{
    public class JsonHelpers
    {
        async public static ValueTask<T?> DeserializeAsync<T>(byte[] jData)
        {
            MemoryStream? stream = new(jData);

            return await DeserializeAsync<T>(stream);

        }
        async public static ValueTask<T?> DeserializeAsync<T>(MemoryStream stream)
        {
            var data = await JsonSerializer.DeserializeAsync<T>(stream, JsonSerializeOptionsProvider.Default());

            return data;
        }

        async public static ValueTask<T?> DeserializeAsync<T>(Stream stream)
        {
            var data = await JsonSerializer.DeserializeAsync<T>(stream, JsonSerializeOptionsProvider.Default());

            return data;
        }
    }
}
