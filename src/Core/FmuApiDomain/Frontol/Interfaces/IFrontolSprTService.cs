using CSharpFunctionalExtensions;

namespace FmuApiDomain.Frontol.Interfaces
{
    public interface IFrontolSprTService
    {
        Task<Result<int>> PrintGroupCodeByBarcodeAsync(string barCode);
    }
}
