using Shared.Json;

namespace Shared.Http
{
    public class HttpHelpers
    {
        async public static ValueTask<T?> GetJsonFromHttpAsync<T>(string url, Dictionary<string, string> headers, IHttpClientFactory httpClientFactory, TimeSpan timeout)
        {
            var httpRequestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                url);

            foreach (var header in headers)
            {
                httpRequestMessage.Headers.Add(header.Key, header.Value);
            }

            var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = timeout;
            var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);

            Stream? stream = await httpResponseMessage.Content.ReadAsStreamAsync();

            if (stream is null)
                return default;

            return await JsonHelpers.DeserializeAsync<T>(stream);
        }

        async public static Task<string> GetHttpAsync(string url, Dictionary<string, string> headers, IHttpClientFactory httpClientFactory, TimeSpan timeout)
        {
            var httpRequestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                url);

            foreach (var header in headers)
            {
                httpRequestMessage.Headers.Add(header.Key, header.Value);
            }

            var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = timeout;
            var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);

            Stream? stream = await httpResponseMessage.Content.ReadAsStreamAsync();

            if (stream is null)
                return string.Empty;

            StreamReader reader = new(stream);
            return await reader.ReadToEndAsync();
        }

        async public static ValueTask<T?> PostAsync<T>(string url, Dictionary<string, string> headers, HttpContent content, IHttpClientFactory httpClientFactory, TimeSpan timeout)
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };

            foreach (var header in headers)
            {
                httpRequestMessage.Headers.Add(header.Key, header.Value);
            }

            var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = timeout;

            var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);

            Stream? stream = await httpResponseMessage.Content.ReadAsStreamAsync();

            return await JsonHelpers.DeserializeAsync<T>(stream);
        }
    }
}
