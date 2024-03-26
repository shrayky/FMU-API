using CSharpFunctionalExtensions;
using FmuApiApplication.Utilites;
using FmuApiDomain.Models.TrueSignApi;
using FmuApiDomain.Models.TrueSignApi.ProductInfo;
using FmuApiSettings;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Json;

namespace FmuApiApplication.Services.TrueSign
{
    public class ProductInfo
    {
        private readonly string _addres = "https://markirovka.crpt.ru/api/v4/true-api/product/info";
        private readonly int requestTimeoutSeconds = 5;

        private readonly ILogger<ProductInfo> _logger;
        private IHttpClientFactory _httpClientFactory;

        public ProductInfo(IHttpClientFactory httpClientFactory, ILogger<ProductInfo> logger)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<Result<ProductsInformationTrueApi>> Load(List<string> gtins)
        {
            Dictionary<string, string> headers = new()
            {
                { HeaderNames.Accept, "application/json"},
                { HeaderNames.CacheControl, "no-cache"},
                { HeaderNames.Authorization, $"Bearer {Constants.Parametrs.SignData.Token()}"}
            };

            try
            {
                var infomation = await HttpRequestHelper.PostAsync<ProductsInformationTrueApi>(_addres,
                                                                                               headers,
                                                                                               JsonContent.Create(new GtinsArray(gtins)),
                                                                                               _httpClientFactory,
                                                                                               TimeSpan.FromSeconds(requestTimeoutSeconds));

                if (infomation == null)
                    return Result.Failure<ProductsInformationTrueApi>("Ошибка выполенения запроса");
                    
                return Result.Success(infomation);

            }
            catch (Exception ex)
            {
                return Result.Failure<ProductsInformationTrueApi>($"Ошибка выполенения запроса {ex.Message}");
            }
            
        }
    }
}
