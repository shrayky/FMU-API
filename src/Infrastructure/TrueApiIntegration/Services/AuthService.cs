using CryptoPro.Security.Cryptography;
using CryptoPro.Security.Cryptography.Pkcs;
using CryptoPro.Security.Cryptography.X509Certificates;
using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Json;
using System.Net.Http.Json;
using System.Security;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using TrueApiIntegration.Interfaces;
using TrueApiIntegration.Models;

namespace TrueApiIntegration.Services;

[AutoRegisterService(ServiceLifetime.Singleton)]
public class AuthService : IAuthService
{
    private readonly ILogger<AuthService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    private string URL = @"https://markirovka.crpt.ru";
    private string AUTHPATH = @"/api/v3/true-api/auth/key";
    private string SIGNINPATH = @"/api/v3/true-api/auth/simpleSignIn";

    public AuthService(ILogger<AuthService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> GenerateToken(string inn, string password, string signatureNaumber)
    {
        var data = await DataForEncrypt();

        if (data.IsFailure)
            return string.Empty;

        var dataForEncrypt = data.Value.Data;
        var uuidHandShacke = data.Value.Uuid;

        var encrypted = Encrypt(dataForEncrypt, signatureNaumber, inn, password);

        if (encrypted.IsFailure)
            return string.Empty;

        var encryptedData = encrypted.Value;

        var tokenData = await FinishAuth(encryptedData, uuidHandShacke);

        if (tokenData.IsFailure)
            return string.Empty;

        return tokenData.Value;
    }

    private async Task<Result<DataWithUuid>> DataForEncrypt()
    {
        using var httpClient = _httpClientFactory.CreateClient("TrueApiIntegration");

        httpClient.BaseAddress = new Uri(URL);

        try
        {
            var answer = await httpClient.GetFromJsonAsync<DataWithUuid>(AUTHPATH);

            if (answer == null)
                throw new Exception($"Пустой ответ от {AUTHPATH}");

            return Result.Success(answer);
        }
        catch (Exception ex)
        {
            _logger.LogError("Ошибка получения данных авторизации в true api {err}", ex);

            return Result.Failure<DataWithUuid>($"Ошибка получения данных авторизации в true api {ex.Message}");
        }
    }

    private Result<string> Encrypt(string data, string signatureNumber, string inn, string password)
    {
        var store = new CpX509Store(StoreName.My, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadOnly);

        CpX509Certificate2? certificate = null;

        foreach (var storeCertificate in store.Certificates)
        {
            if (storeCertificate.NotAfter <= DateTime.Now)
                continue;

            if (storeCertificate.Issuer.Contains("DO_NOT_TRUST"))
                continue;


            if (storeCertificate.Subject.Contains("ОГРНИП"))
            {
                if (!storeCertificate.Subject.Contains(inn))
                    continue;
            }
            else
            {
                if (!storeCertificate.Subject.Contains($"ИНН ЮЛ={inn}"))
                    continue;
            }

            if (signatureNumber == string.Empty)
            {
                certificate = storeCertificate;
                break;
            }

            if (storeCertificate.GetSerialNumberString() == signatureNumber)
            {
                certificate = storeCertificate;
                break;
            }
        }

        store.Close();

        if (certificate == null)
        {
            var msg = $"Для ИНН {inn} не найден действующий сертификат";

            _logger.LogError(msg);
            return Result.Failure<string>(msg);
        }

        _logger.LogInformation("Выбран сертификат для авторизации в true api {info}", certificate.Subject);

        try
        {
            var privateKey = certificate.GetGost3410_2012_256PrivateKey()
                            ?? certificate.GetGost3410_2012_512PrivateKey() as Gost3410Algorithm
                            ?? certificate.GetGost3410PrivateKey();

            if (privateKey == null)
                return Result.Failure<string>("Не удалось получить закрытый ключ ГОСТ из сертификата");

            if (!string.IsNullOrEmpty(password))
            {
                var securePassword = new SecureString();
                foreach (char c in password)
                    securePassword.AppendChar(c);
                securePassword.MakeReadOnly();

                if (privateKey is Gost3410_2012_256CryptoServiceProvider csp256)
                    csp256.SetContainerPassword(securePassword);
                else if (privateKey is Gost3410_2012_512CryptoServiceProvider csp512)
                    csp512.SetContainerPassword(securePassword);
                else if (privateKey is Gost3410CryptoServiceProvider csp3410)
                    csp3410.SetContainerPassword(securePassword);
            }

            byte[] dataToSign = Encoding.UTF8.GetBytes(data);
            var contentInfo = new ContentInfo(dataToSign);
            var signedCms = new CpSignedCms(contentInfo, detached: false);

            var signer = new CpCmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, certificate, privateKey);
            signer.IncludeOption = X509IncludeOption.WholeChain;

            signedCms.ComputeSignature(signer, silent: true);

            byte[] signatureBytes = signedCms.Encode();
            string signatureBase64 = Convert.ToBase64String(signatureBytes);
            return Result.Success(signatureBase64);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка подписи при авторизации True API");
            return Result.Failure<string>(ex.Message);
        }
    }

    private async Task<Result<string>> FinishAuth(string encodedData, string requestId)
    {
        using var httpClient = _httpClientFactory.CreateClient("TrueApiIntegration");

        httpClient.BaseAddress = new Uri(URL);

        try
        {
            DataWithUuid data = new()
            {
                Uuid = requestId,
                Data = encodedData
            };

            var answer = await httpClient.PostAsJsonAsync(SIGNINPATH, data);
            
            if (answer == null)
                throw new Exception($"Пустой ответ от {SIGNINPATH}");

            if (!answer.IsSuccessStatusCode)
                throw new Exception($"{SIGNINPATH} вернул код ошибки {answer.StatusCode}");

            var rawJson = await answer.Content.ReadAsStringAsync();
            _logger.LogDebug("Ответ simpleSignIn : {Json}", rawJson);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawJson));
            var answerData = await JsonHelpers.DeserializeAsync<AuthData>(stream);

            if (answerData == null)
                throw new Exception($"Ошибка преобразования ответа в {SIGNINPATH}");

            return Result.Success(answerData.Token);

        }
        catch (Exception ex)
        {
            _logger.LogError("Ошибка получения данных авторизации в true api {err}", ex);

            return Result.Failure<string>($"Ошибка получения данных авторизации в true api {ex.Message}");
        }
    }



}
