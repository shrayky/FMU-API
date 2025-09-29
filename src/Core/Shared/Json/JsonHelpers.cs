using System.Text.Json;

namespace Shared.Json
{
    public static class JsonHelpers
    {
        public static async ValueTask<T?> DeserializeAsync<T>(byte[] jData)
        {
            MemoryStream? stream = new(jData);

            return await DeserializeAsync<T>(stream);

        }
        public static async ValueTask<T?> DeserializeAsync<T>(MemoryStream stream)
        {
            var data = await JsonSerializer.DeserializeAsync<T>(stream, JsonSerializeOptionsProvider.Default());

            return data;
        }

        public static async ValueTask<T?> DeserializeAsync<T>(Stream stream)
        {
            var data = await JsonSerializer.DeserializeAsync<T>(stream, JsonSerializeOptionsProvider.Default());

            return data;
        }

        public static async ValueTask<T?> DeserializeAsync<T>(string json)
        {
            using var stream = new MemoryStream();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(json).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);

            stream.Position = 0;
                
            return await JsonSerializer.DeserializeAsync<T>(stream);
        }
        
        public static async Task<string> SerializeAsync<T>(T obj)
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, obj, JsonSerializeOptionsProvider.Default());
            stream.Position = 0;
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        
        public static async Task<string> SerializeAsync<T>(T obj, JsonSerializerOptions options)
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, obj, options);
            stream.Position = 0;
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        
    }
}
