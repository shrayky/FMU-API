using FmuApiApplication.Utilites;
using FmuApiDomain.Models.Configuration;
using FmuApiDomain.Models.TrueSignApi.MarkData.Check;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Json;

namespace FmuApiApplication.Services.TrueSign
{
    public class CheckMarks
    {
        private readonly string _addres = "/api/v4/true-api/codes/check";
        private readonly int requestTimeoutSeconds = 2;
        private readonly int requestAttempts = 3;

        private readonly ILogger<CheckMarks> _logger;
        private IHttpClientFactory _httpClientFactory;

        public CheckMarks(ILogger<CheckMarks> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<CheckAnswerTrueApi?> RequestMarkState(CheckMarksRequestData marks)
        {
            if (!Constants.Online)
                return null;

            Dictionary<string, string> headers = new();
            headers.Add(HeaderNames.Accept, "application/json");

            if (Constants.Parametrs.SignData.Token() == "")
                headers.Add("X-API-KEY", Constants.Parametrs.XAPIKEY);
            else
                headers.Add(HeaderNames.Authorization, $"Bearer {Constants.Parametrs.SignData.Token()}");

            int attemptLost = requestAttempts;

            while (true)
            {
                TrueSignCdn? cdn = Cdn();

                if (cdn is null)
                    return null;

                try
                {
                    var answ = await HttpRequestHelper.PostAsync<CheckAnswerTrueApi>($"{cdn.Host}{_addres}",
                                                                                    headers,
                                                                                    JsonContent.Create(marks),
                                                                                    _httpClientFactory,
                                                                                    TimeSpan.FromSeconds(requestTimeoutSeconds));

                    return answ;
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

            return null;

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
