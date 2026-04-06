using FmuApiDomain.TrueApiIntegration.Models;

namespace FmuApiDomain.TrueApiIntegration.Interfaces;

public interface IDigtalSignatureService
{
    List<DigitalSignature> List();
}
