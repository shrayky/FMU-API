using CryptoPro.Security.Cryptography;
using CryptoPro.Security.Cryptography.Pkcs;
using CryptoPro.Security.Cryptography.X509Certificates;
using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.GisMt.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace TrueApiIntegration.Services;

/// <summary>
/// Откреплённая подпись документа ГИС МТ сертификатом организации из хранилища КриптоПро.
/// </summary>
[AutoRegisterService(ServiceLifetime.Singleton)]
public class GisMtDocumentSigner : IGisMtDocumentSigner
{
    private readonly ILogger<GisMtDocumentSigner> _logger;

    public GisMtDocumentSigner(ILogger<GisMtDocumentSigner> logger)
    {
        _logger = logger;
    }

    public Result<string> SignDetached(string inn, string certificateNumber, string password, string payload)
    {
        var certificateResult = FindCertificate(inn, certificateNumber);

        if (certificateResult.IsFailure)
            return Result.Failure<string>(certificateResult.Error);

        var certificate = certificateResult.Value;

        var privateKey = certificate.GetGost3410_2012_256PrivateKey()
                         ?? certificate.GetGost3410_2012_512PrivateKey() as Gost3410Algorithm
                         ?? certificate.GetGost3410PrivateKey();

        if (privateKey == null)
            return Result.Failure<string>("Не удалось получить закрытый ключ ГОСТ из сертификата");

        if (!string.IsNullOrEmpty(password))
        {
            var securePassword = new SecureString();
            foreach (var character in password)
                securePassword.AppendChar(character);
            securePassword.MakeReadOnly();

            if (privateKey is Gost3410_2012_256CryptoServiceProvider csp256)
                csp256.SetContainerPassword(securePassword);
            else if (privateKey is Gost3410_2012_512CryptoServiceProvider csp512)
                csp512.SetContainerPassword(securePassword);
            else if (privateKey is Gost3410CryptoServiceProvider csp3410)
                csp3410.SetContainerPassword(securePassword);
        }

        var contentInfo = new ContentInfo(Encoding.UTF8.GetBytes(payload));
        var signedCms = new CpSignedCms(contentInfo, detached: true);
        var signer = new CpCmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, certificate, privateKey);
        signer.IncludeOption = X509IncludeOption.EndCertOnly;

        signedCms.ComputeSignature(signer, silent: true);

        return Result.Success(Convert.ToBase64String(signedCms.Encode()));
    }

    private Result<CpX509Certificate2> FindCertificate(string inn, string certificateNumber)
    {
        var store = new CpX509Store(StoreName.My, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadOnly);

        CpX509Certificate2? found = null;

        foreach (var certificate in store.Certificates)
        {
            if (certificate.NotAfter <= DateTime.Now)
                continue;

            if (certificate.Issuer.Contains("DO_NOT_TRUST"))
                continue;

            if (certificate.GetSerialNumberString() == certificateNumber)
            {
                found = certificate;
                break;
            }

            if (string.IsNullOrEmpty(certificateNumber) && certificate.Subject.Contains(inn))
            {
                found = certificate;
                break;
            }
        }

        store.Close();

        if (found == null)
        {
            _logger.LogError("Для ИНН {Inn} не найден действующий сертификат для подписи документа", inn);
            return Result.Failure<CpX509Certificate2>($"Для ИНН {inn} не найден действующий сертификат");
        }

        return Result.Success(found);
    }
}
