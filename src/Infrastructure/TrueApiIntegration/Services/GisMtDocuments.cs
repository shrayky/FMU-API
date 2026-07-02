using Microsoft.Extensions.Logging;
using TrueApiIntegration.Models;

namespace TrueApiIntegration.Services;

public class GisMtDocuments
{
    private ILogger<GisMtDocuments> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    private string URL = @"https://markirovka.crpt.ru";
    private string LIST_PATH = @"/api/v4/true-api/doc/list";

    public GisMtDocuments(ILogger<GisMtDocuments> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }


    public List<string> List(DateTime from, DateTime to, string receiverInn)
    {
        _logger.LogInformation("Получаю документы из ГИС МТ по инн {inn} с {from} по {to}", receiverInn, from, to);

        using var httpClient = _httpClientFactory.CreateClient("TrueApiIntegration");

        httpClient.BaseAddress = new Uri(URL);

        var parameters = new RequestParameters()
        {
            DateFrom = from,
            DateTo = to,
            DocumentFormat = "json",
            ReceiverInn = receiverInn
        };

        try
        {

        }
        catch (Exception ex)
        {
        }

        return new List<string>();
    }

}
