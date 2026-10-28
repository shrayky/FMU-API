namespace FmuApiDomain.ProductGroups.Interfaces;

/// <summary>
/// Определяет товарную группу Честного знака и признаки проверок ЕМЦ, МРЦ, срока годности и запрета продажи по сроку.
/// </summary>
public interface IProductGroupResolver
{
    Task<int?> Resolve(int atolItemType, string gtin);

    bool ShouldCheckSmp(int atolItemType, int trueApiGroupId);

    bool ShouldCheckMrp(int atolItemType, int trueApiGroupId);

    bool ShouldCheckExpireDate(int atolItemType, int trueApiGroupId);

    bool ShouldHaveExpireDate(int atolItemType, int trueApiGroupId);

    bool ShouldForbidSaleByExpireDate(int atolItemType, int trueApiGroupId);
}
