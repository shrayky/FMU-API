using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.MarkingCalendar;

/// <summary>
/// Прокси календаря внедрения с сайта Честный знак. Нужен из-за CORS в браузере.
/// </summary>
[Route("api/marking-calendar")]
[ApiController]
[ApiExplorerSettings(GroupName = "App configuration")]
public class MarkingCalendarController(IHttpClientFactory httpClientFactory) : ControllerBase
{
    private const string HttpClientName = "ChestnyZnak";
    private const string SourceUrl =
        "https://xn--80ajghhoc2aj1c8b.xn--p1ai/bitrix/services/main/ajax.php?mode=class&c=dev:markingCalendar&action=getSheduleList";

    /// <summary>
    /// Возвращает JSON календаря внедрения без изменений.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        try
        {
            using var client = httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, SourceUrl);
            request.Headers.TryAddWithoutValidation("Referer", "https://xn--80ajghhoc2aj1c8b.xn--p1ai/");
            request.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, "Не удалось загрузить календарь внедрения");

            return Content(body, "application/json", Encoding.UTF8);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
