namespace FmuApiDomain.ProductGroups.Interfaces;

/// <summary>
/// Определяет товарную группу Честного знака и признаки проверок ЕМЦ, МРЦ и срока годности.
/// </summary>
public interface IProductGroupResolver
{
    Task<int?> ResolveAsync(int atolItemType, string gtin);

    bool ShouldCheckSmp(int atolItemType, int trueApiGroupId);

    bool ShouldCheckMrp(int atolItemType, int trueApiGroupId);

    bool ShouldCheckExpireDate(int atolItemType, int trueApiGroupId);
}
