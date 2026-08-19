namespace FmuApiDomain.ProductGroups.Interfaces;

public interface IProductGroupResolver
{
    Task<int?> ResolveAsync(int atolItemType, string gtin);

    bool ShouldCheckSmp(int atolItemType, int trueApiGroupId);
}
