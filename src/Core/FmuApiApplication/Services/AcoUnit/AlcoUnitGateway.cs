using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Token;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

namespace FmuApiApplication.Services.AcoUnit
{
    public class AlcoUnitGateway
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IParametersService _parameters;
        private readonly ILogger<AlcoUnitGateway> _logger;
        private readonly AlcoUnitConfig frontolAlcoUnit;

        public AlcoUnitGateway(IHttpClientFactory httpClientFactory, IParametersService parameters, ILogger<AlcoUnitGateway> logger)
        {
            _httpClientFactory = httpClientFactory;
            _parameters = parameters;
            _logger = logger;

            frontolAlcoUnit = parameters.Current().FrontolAlcoUnit;
        }

        private async Task CheckTokenAsync()
        {
            if (frontolAlcoUnit.Token == string.Empty)
                await AuthtorithationAsync();

            if (frontolAlcoUnit.Token == string.Empty)
                throw new Exception("Не получен токен авторизации для алкоюнита!");
        }

        private HttpRequestMessage CreateHttpMessage(HttpMethod httpMethod, string requestAdres)
        {
            Dictionary<string, string> headers = new()
            {
                { HeaderNames.Accept, "application/json" },
                { HeaderNames.Authorization, $"Bearer {frontolAlcoUnit.Token}" },
            };

            var message = new HttpRequestMessage(httpMethod, requestAdres);

            foreach (var header in headers)
            {
                message.Headers.Add(header.Key, header.Value);
            }

            return message;
        }

        private async Task<HttpResponseMessage> SendHttpMessageAsync(HttpRequestMessage httpRequestMessage)
        {
            var client = _httpClientFactory.CreateClient("alcoUnit");

            int attemptCount = 2;

            HttpResponseMessage answer = new();

            while (attemptCount > 0)
            {
                attemptCount--;

                answer = await client.SendAsync(httpRequestMessage);

                if (answer.StatusCode == System.Net.HttpStatusCode.Unauthorized && attemptCount > 0)
                {
                    frontolAlcoUnit.Token = "";
                    await AuthtorithationAsync();
                    continue;
                }

                break;
            }

            return answer;
        }

        public async Task<string> AuthtorithationAsync()
        {
            if (frontolAlcoUnit.NetAdres == string.Empty)
                throw new Exception("Не задан сетевой адрес алкоюнита!");

            var hash = MD5.HashData(Encoding.ASCII.GetBytes($"{frontolAlcoUnit.UserName}:{frontolAlcoUnit.Password}"));
            string id = Convert.ToHexString(hash);

            AuthorizationData authData = new()
            {
                Id = frontolAlcoUnit.UserName,
                Password = id.ToLower()
            };

            var hashedAuthData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(authData.ToString()));

            Dictionary<string, string> headers = new()
            {
                { HeaderNames.Accept, "application/json" },
                { HeaderNames.Authorization, $"Direct {hashedAuthData}" },
            };

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "token");

            foreach (var header in headers)
            {
                httpRequestMessage.Headers.Add(header.Key, header.Value);
            }

            var client = _httpClientFactory.CreateClient("alcoUnit");
            var answer = await client.SendAsync(httpRequestMessage);

            if (!answer.IsSuccessStatusCode)
                throw new Exception($"Запос авторизации вернул код {answer.StatusCode}");

            var content = await answer.Content.ReadAsStringAsync();
            var token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content));

            frontolAlcoUnit.Token = token;

            return token;
        }

        public async Task<string> ProductsByStamp(string stamp)
        {
            await CheckTokenAsync();

            HttpRequestMessage httpRequestMessage = CreateHttpMessage(HttpMethod.Get, $"products_by_stamp/{stamp}");

            HttpResponseMessage answer = await SendHttpMessageAsync(httpRequestMessage);

            if (!answer.IsSuccessStatusCode)
                throw new Exception($"Запос товара по алкомарке вернул код {answer.StatusCode}");

            return await answer.Content.ReadAsStringAsync();
        }

        public async Task<string> SendDDocumentAsync(RequestDocument document)
        {
            await CheckTokenAsync();

            HttpRequestMessage httpRequestMessage = CreateHttpMessage(HttpMethod.Post, "document");
            httpRequestMessage.Content = JsonContent.Create(document);

            _logger.LogInformation("Отправляем в alocunit запрос {@content}", document);

            HttpResponseMessage answer = await SendHttpMessageAsync(httpRequestMessage);

            if (!answer.IsSuccessStatusCode)
                throw new Exception($"Отправка документа вернула код {answer.StatusCode}");

            return await answer.Content.ReadAsStringAsync();
        }
    }
}
