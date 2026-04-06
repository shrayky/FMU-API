using CryptoPro.Security.Cryptography.X509Certificates;
using FmuApiDomain.Attributes;
using FmuApiDomain.TrueApiIntegration.Interfaces;
using FmuApiDomain.TrueApiIntegration.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography.X509Certificates;

namespace TrueApiIntegration.Services;

[AutoRegisterService(ServiceLifetime.Singleton)]
public class DigtalSignatureServices : IDigtalSignatureService
{
    public List<DigitalSignature> List()
    {
        var answer = new List<DigitalSignature>();

        var store = new CpX509Store(StoreName.My, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadOnly);

        foreach (var certificate in store.Certificates)
        {
            if (certificate.NotAfter <= DateTime.Now)
                continue;

            if (certificate.Issuer.Contains("DO_NOT_TRUST"))
                continue;

            var signature = new DigitalSignature
            {
                Presentation = certificate.Subject,
                WorkUntil = certificate.NotAfter,
                Number = certificate.GetSerialNumberString()
            };

            var subjectLines = certificate.Subject.Split(",");

            var lineWithInn = subjectLines.FirstOrDefault(l => l.Contains("ИНН ЮЛ"));
            lineWithInn ??= subjectLines.FirstOrDefault(l => l.Contains("ИНН"));

            if (lineWithInn != null)
            {
                var parts = lineWithInn.Split("=");

                if (parts.Length >= 2)
                    signature.Inn = parts[1];
            }

            answer.Add(signature);
        }

        store.Close();

        return answer;
    }
}
