using System.Text.Json;
using FmuApiDomain.GisMt.Journal;
using FmuApiDomain.GisMt.Models;
using Xunit;

namespace FmuApiApplication.Tests;

public class GisMtDocInfoResponseTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void Нулевые_ошибки_в_ответе_читаются_как_пустой_список()
    {
        const string json = """{"number":"doc-1","status":"CHECKED_OK","errors":null}""";

        var info = JsonSerializer.Deserialize<GisMtDocInfoResponse>(json, JsonOptions)!;

        Assert.Null(info.Errors);
        Assert.Empty(info.ErrorMessages);
        Assert.Equal(GisMtCrptStatuses.CheckedOk, info.Status);
    }

    [Fact]
    public void Ошибки_обработки_читаются_списком()
    {
        const string json = """{"number":"doc-1","status":"CHECKED_NOT_OK","errors":["16: ошибка"]}""";

        var info = JsonSerializer.Deserialize<GisMtDocInfoResponse>(json, JsonOptions)!;

        Assert.Equal(new[] { "16: ошибка" }, info.ErrorMessages);
    }

    [Fact]
    public void Без_поля_ошибок_список_пуст()
    {
        const string json = """{"number":"doc-1","status":"IN_PROGRESS"}""";

        var info = JsonSerializer.Deserialize<GisMtDocInfoResponse>(json, JsonOptions)!;

        Assert.Empty(info.ErrorMessages);
    }
}
