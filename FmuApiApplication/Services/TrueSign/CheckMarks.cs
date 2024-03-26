using CSharpFunctionalExtensions;
using FmuApiApplication.Utilites;
using FmuApiDomain.Models.Configuration;
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
        private readonly int requestAttempts = Constants.Parametrs.Cdn.Count;

        private readonly ILogger<CheckMarks> _logger;
        private IHttpClientFactory _httpClientFactory;

        public CheckMarks(ILogger<CheckMarks> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<Result<CheckAnswerTrueApi>> RequestMarkState(CheckMarksRequestData marks)
        {
            if (!Constants.Online)
                return Result.Failure<CheckAnswerTrueApi>("Нет интеренета");

            if (Constants.Parametrs.Cdn.Count == 0)
                return Result.Failure<CheckAnswerTrueApi>("Нет загруженных cdn");

            Dictionary<string, string> headers = new();
            headers.Add(HeaderNames.Accept, "application/json");

            if (Constants.Parametrs.SignData.Token() == "")
                headers.Add("X-API-KEY", Constants.Parametrs.XAPIKEY);
            else
                headers.Add(HeaderNames.Authorization, $"Bearer {Constants.Parametrs.SignData.Token()}");

            int attemptLost = requestAttempts;

            var content = JsonContent.Create(marks);

            _logger.LogInformation("Проверяю марки в честном знаке {@request}", content);

            while (true)
            {
                TrueSignCdn? cdn = Cdn();

                if (cdn is null)
                    return Result.Failure<CheckAnswerTrueApi>("Нет загруженных cdn");

                try
                {
                    var answ = await HttpRequestHelper.PostAsync<CheckAnswerTrueApi>($"{cdn.Host}{_addres}",
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

            return Result.Failure<CheckAnswerTrueApi>("Ни один cdn сервер не ответил");

        }

        private static TrueSignCdn? Cdn()
        {
            if (Constants.Parametrs.Cdn.Count == 0)
                return null;

            foreach (var cdn in Constants.Parametrs.Cdn)
            {
                if (!cdn.IsOffline)
                    continue;

                if ((DateTime.Now - cdn.OfflineSetDate).TotalMinutes > 15)
                    cdn.BringOnline();
            }

            var cdns = Constants.Parametrs.Cdn.Where(p => p.IsOffline == false).ToList();

            if (cdns.Count == 0)
            {
                foreach (var cdn in Constants.Parametrs.Cdn)
                {
                    cdn.BringOnline();
                }
            }

            foreach (var cdn in Constants.Parametrs.Cdn)
            {
                if (cdn.IsOffline)
                    continue;

                return cdn;
            }

            return null;
        }
    }
}
