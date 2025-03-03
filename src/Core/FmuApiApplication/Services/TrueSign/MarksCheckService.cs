﻿using CSharpFunctionalExtensions;
using FmuApiDomain.Cdn;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.State.Interfaces;
using FmuApiDomain.TrueApi.MarkData.Check;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Shared.Http;
using System.Net.Http.Json;
using TrueApiCdn.Interface;

namespace FmuApiApplication.Services.TrueSign
{
    public class MarksCheckService
    {
        private readonly string _address = "/api/v4/true-api/codes/check";
        private readonly int requestTimeoutSeconds = 2;
        private readonly int requestAttempts = 1;

        private readonly ILogger<MarksCheckService> _logger;
        private IHttpClientFactory _httpClientFactory;
        private readonly IParametersService _parametersService;
        private readonly ICdnService _cdnService;
        private readonly IApplicationState _applicationState;

        private Parameters _configuration;
        
        public MarksCheckService(IParametersService parametersService,
                                 ICdnService cdnService,
                                 IHttpClientFactory httpClientFactory,
                                 IApplicationState applicationState,
                                 ILogger<MarksCheckService> logger)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _parametersService = parametersService;
            _cdnService = cdnService;
            _applicationState = applicationState;

            _configuration = _parametersService.Current();
            requestTimeoutSeconds = _configuration.HttpRequestTimeouts.CheckMarkRequestTimeout;
        }

        public async Task<Result<CheckMarksDataTrueApi>> RequestMarkState(CheckMarksRequestData marks, int organizationCode)
        {
            string xApiKey;

            if (organizationCode == 0)
                xApiKey = _configuration.OrganisationConfig.XapiKey() ?? "";
            else
                xApiKey = _configuration.OrganisationConfig.XapiKey(organizationCode) ?? "";

            if (string.IsNullOrEmpty(xApiKey))
                return Result.Failure<CheckMarksDataTrueApi>($"Не получен XAPIKEY для организации с кодом {organizationCode}");

            return await DoRequest(marks, xApiKey!);
        }

        public async Task<Result<CheckMarksDataTrueApi>> RequestMarkState(CheckMarksRequestData marks)
        {
            string xApiKey = _configuration.OrganisationConfig.XapiKey();

            return await DoRequest(marks, xApiKey);
        }

        private async Task<Result<CheckMarksDataTrueApi>> DoRequest(CheckMarksRequestData marks, string xApiKey)
        {
            if (!_applicationState.IsOnline())
                return Result.Failure<CheckMarksDataTrueApi>("Нет интернета");

            IReadOnlyList<TrueSignCdn> cdns = await _cdnService.GetCdnsAsync();

            if (cdns.Count == 0)
                return Result.Failure<CheckMarksDataTrueApi>("Нет загруженных cdn");

            TrueSignCdn? cdn = await _cdnService.GetActiveCdnAsync(0);

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
                    var answer = await HttpHelpers.PostAsync<CheckMarksDataTrueApi>($"{cdn.Host}{_address}",
                                                                                    headers,
                                                                                    content,
                                                                                    _httpClientFactory,
                                                                                    TimeSpan.FromSeconds(requestTimeoutSeconds));
                    if (answer is null)
                        continue;

                    _logger.LogInformation("Получен ответ от честного знака {@answer}", answer);

                    return Result.Success(answer);
                }
                catch
                {
                    _logger.LogWarning("{Host} не ответил за {requestTimeoutSeconds} секунды, помечаем его offline", cdn.Host, requestTimeoutSeconds);
                    cdn.BringOffline();
                    attemptLost--;
                }

                if (attemptLost == 0)
                    break;
            }

            return Result.Failure<CheckMarksDataTrueApi>("Ни один cdn сервер не ответил.");
        }
    }
}
