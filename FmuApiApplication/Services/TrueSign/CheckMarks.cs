using CSharpFunctionalExtensions;
using FmuApiApplication.Utilites;
using FmuApiDomain.Models.Configuration.TrueSign;
using FmuApiDomain.Models.TrueSignApi.MarkData.Check;
using FmuApiSettings;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Json;

namespace FmuApiApplication.Services.TrueSign
{
    public class CheckMarks
    {
        private readonly string _addres = "/api/v4/true-api/codes/check";
        private readonly int requestTimeoutSeconds = Constants.Parametrs.HttpRequestTimeouts.CheckMarkRequestTimeout;
        private readonly int requestAttempts = 1;

        private readonly ILogger<CheckMarks> _logger;
        private IHttpClientFactory _httpClientFactory;

        public CheckMarks(ILogger<CheckMarks> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<Result<CheckMarksDataTrueApi>> RequestMarkState(CheckMarksRequestData marks, int ogranisationCode)
        {
            string xApiKey;

            if (ogranisationCode == 0)
                xApiKey = Constants.Parametrs.OrganisationConfig.XapiKey();
            else
                xApiKey = Constants.Parametrs.OrganisationConfig.XapiKey(ogranisationCode);

            _logger.LogInformation(xApiKey);

            return await DoRequest(marks, xApiKey);
        }

        public async Task<Result<CheckMarksDataTrueApi>> RequestMarkState(CheckMarksRequestData marks)
        {
            string xApiKey = Constants.Parametrs.OrganisationConfig.XapiKey();

            return await DoRequest(marks, xApiKey);
        }

        private async Task<Result<CheckMarksDataTrueApi>> DoRequest(CheckMarksRequestData marks, string xApiKey)
        {
            if (!Constants.Online)
                return Result.Failure<CheckMarksDataTrueApi>("Нет интеренета");

            if (Constants.Cdn.List.Count == 0)
                return Result.Failure<CheckMarksDataTrueApi>("Нет загруженных cdn");

            TrueSignCdn? cdn = Cdn();

            if (cdn is null)
                return Result.Failure<CheckMarksDataTrueApi>("Нет загруженных cdn");

            Dictionary<string, string> headers = new();
            headers.Add(HeaderNames.Accept, "application/json");

            headers.Add("X-API-KEY", xApiKey);
            
            //headers.Add(HeaderNames.Authorization, $"Bearer {Constants.TrueApiToken.Token()}");

            int attemptLost = requestAttempts;

            var content = JsonContent.Create(marks);

            _logger.LogInformation("Проверяю марки в честном знаке {@request}", content);

            while (true)
            {
                try
                {
                    var answ = await HttpRequestHelper.PostAsync<CheckMarksDataTrueApi>($"{cdn.Host}{_addres}",
                                                                                    headers,
                                                                                    content,
                                                                                    _httpClientFactory,
                                                                                    TimeSpan.FromSeconds(requestTimeoutSeconds));
                    if (answ is null)
                        continue;

                    _logger.LogInformation("Получен ответ от четного знака {@answ}", answ);

                    return Result.Success(answ);
                }
                catch
                {
                    _logger.LogWarning($"[{DateTime.Now}] {cdn.Host} не ответил за {requestTimeoutSeconds} секунды, помечаем его offline");
                    cdn.BringOffline();
                    attemptLost--;
                }

                if (attemptLost == 0)
                    break;
            }

            return Result.Failure<CheckMarksDataTrueApi>("Ни один cdn сервер не ответил");
        }

        private static TrueSignCdn? Cdn()
        {
            if (Constants.Cdn.List.Count == 0)
                return null;

            foreach (var cdn in Constants.Cdn.List)
            {
                if (!cdn.IsOffline)
                    continue;

                if ((DateTime.Now - cdn.OfflineSetDate).TotalMinutes > 15)
                    cdn.BringOnline();
            }

            var cdns = Constants.Cdn.List.Where(p => p.IsOffline == false).ToList();

            if (cdns.Count == 0)
            {
                foreach (var cdn in Constants.Cdn.List)
                {
                    cdn.BringOnline();
                }
            }

            foreach (var cdn in Constants.Cdn.List)
            {
                if (cdn.IsOffline)
                    continue;

                return cdn;
            }

            return null;
        }
    }
}
