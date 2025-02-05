using CSharpFunctionalExtensions;
using FmuApiDomain.TrueApi.ProductInfo;
using FmuApiDomain.TrueApi;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Shared.Http;
using System.Net.Http.Json;
using FmuApiDomain.State.Interfaces;

namespace FmuApiApplication.Services.TrueSign
{
    public class ProductInfo
    {
        private readonly string _address = "https://markirovka.crpt.ru/api/v4/true-api/product/info";
        private readonly int requestTimeoutSeconds = 5;

        private readonly ILogger<ProductInfo> _logger;
        private IHttpClientFactory _httpClientFactory;
        private readonly IApplicationState _applicationState;

        public ProductInfo(IHttpClientFactory httpClientFactory, IApplicationState applicationState, ILogger<ProductInfo> logger)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _applicationState = applicationState;
        }

        public async Task<Result<ProductsInformationTrueApi>> Load(List<string> gtins)
        {
            Dictionary<string, string> headers = new()
            {
                { HeaderNames.Accept, "application/json"},
                { HeaderNames.CacheControl, "no-cache"},
                { HeaderNames.Authorization, $"Bearer {_applicationState.TrueApiToken}"}
            };

            try
            {
                var information = await HttpHelpers.PostAsync<ProductsInformationTrueApi>(_address,
                                                                                         headers,
                                                                                         JsonContent.Create(new GtinsArray(gtins)),
                                                                                              _httpClientFactory,
                                                                                         TimeSpan.FromSeconds(requestTimeoutSeconds));

                if (information == null)
                    return Result.Failure<ProductsInformationTrueApi>("Ошибка выполнения запроса");
                    
                return Result.Success(information);

            }
            catch (Exception ex)
            {
                return Result.Failure<ProductsInformationTrueApi>($"Ошибка выполнения запроса {ex.Message}");
            }
            
        }
    }
}
