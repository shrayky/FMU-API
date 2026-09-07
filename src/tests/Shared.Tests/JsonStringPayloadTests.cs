using System.Text.Json;
using Shared.Json;
using Shared.Strings;
using Xunit;

namespace Shared.Tests;

public class JsonStringPayloadTests
{
    [Fact]
    public void Unwrap_снимает_кавычки_как_у_Ok_string()
    {
        const string payload = "abc+def/ghi==";
        var httpBody = JsonSerializer.Serialize(payload);

        var unwrapped = JsonStringPayload.Unwrap(httpBody);

        Assert.Equal(payload, unwrapped);
    }

    [Fact]
    public void Unwrap_не_трогает_json_объект()
    {
        const string json = """{"version":1}""";

        var unwrapped = JsonStringPayload.Unwrap(json);

        Assert.Equal(json, unwrapped);
    }

    [Fact]
    public void Decrypt_после_обёртки_Ok_string()
    {
        const string secret = "test-secret";
        const string plain = """{"version":12}""";
        var encrypted = SecretString.EncryptData(plain, secret);
        var httpBody = JsonSerializer.Serialize(encrypted);

        var decrypted = SecretString.DecryptData(JsonStringPayload.Unwrap(httpBody), secret);

        Assert.Equal(plain, decrypted);
    }

    [Fact]
    public void Decrypt_принимает_пробелы_вокруг_base64()
    {
        const string secret = "test-secret";
        const string plain = "payload";
        var encrypted = SecretString.EncryptData(plain, secret);

        var decrypted = SecretString.DecryptData($"\n{encrypted}\r\n", secret);

        Assert.Equal(plain, decrypted);
    }
}
