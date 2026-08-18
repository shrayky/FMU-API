namespace FmuApiDomain.ProductGroups.Interfaces;

public interface IProductGroupResolver
{
    /// <summary>
    /// Определяет код товарной группы Честного знака: сначала item_type Атол, затем каталог GTIN.
    /// </summary>
    Task<int?> ResolveAsync(int atolItemType, string gtin);

    /// <summary>
    /// Нужно ли проверять ЕМЦ (smp) для позиции.
    /// </summary>
    bool ShouldCheckSmp(int atolItemType, int trueApiGroupId);
}
