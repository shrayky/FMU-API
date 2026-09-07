namespace FmuApiDomain.Frontol;

/// <summary>
/// Тип товара Frontol (SPRT.WARETYPE). 0, 1 и 7 — немаркированная продукция.
/// </summary>
public static class FrontolWareType
{
    public static bool IsUnmarked(int wareType) => wareType is 0 or 1 or 7;

    public static bool IsMarked(int wareType) => !IsUnmarked(wareType);
}
