using FmuApiDomain.Configuration.Options;

namespace FmuApiDomain.TrueApi.MarkData;

/// <summary>
/// Соответствие кодов товарных групп Frontol (item_type) кодам Честного знака.
/// </summary>
public static class AtolToTrueApiGroupMap
{
    public static IReadOnlyDictionary<int, GisMtProductMapping> Defaults { get; } =
        new Dictionary<int, GisMtProductMapping>
        {
            [2] = Create(2, TrueApiGroup.Furs, "Изделия из меха"),
            [3] = Create(3, TrueApiGroup.Pharmaraw, "Лекарственные препараты"),
            [4] = Create(4, TrueApiGroup.Tobaco, "Табачная продукция"),
            [5] = Create(5, TrueApiGroup.Shoes, "Обувь"),
            [8] = Create(8, TrueApiGroup.Electronics, "Фототовары"),
            [9] = Create(9, TrueApiGroup.Perfumery, "Парфюмерная продукция"),
            [10] = Create(10, TrueApiGroup.Tires, "Шины"),
            [11] = Create(11, TrueApiGroup.Lp, "Товары легкой промышленности"),
            [12] = Create(12, TrueApiGroup.Otp, "Альтернативная табачная продукция"),
            [13] = Create(13, TrueApiGroup.Milk, "Молочная продукция"),
            [15] = Create(15, TrueApiGroup.Water, "Вода"),
            [16] = Create(16, TrueApiGroup.Ncp, "Никотиносодержащая продукция"),
            [17] = Create(17, TrueApiGroup.Beer, "Фасованное пиво"),
            [18] = Create(18, TrueApiGroup.Beer, "Разливное пиво"),
            [19] = Create(19, TrueApiGroup.Bio, "БАДы"),
            [20] = Create(20, TrueApiGroup.Antiseptic, "Антисептики"),
            [21] = Create(21, TrueApiGroup.Wheelchairs, "Медицинские изделия"),
            [22] = Create(22, TrueApiGroup.Wheelchairs, "Кресла-коляски"),
            [23] = Create(23, TrueApiGroup.Softdrinks, "Безалкогольные напитки"),
            [24] = Create(24, TrueApiGroup.Wheelchairs, "Средства реабилитации"),
            [25] = Create(25, TrueApiGroup.Nabeer, "Безалкогольное пиво"),
            [26] = Create(26, TrueApiGroup.Seafood, "Икра осетровых и лососевых рыб"),
            [27] = Create(27, TrueApiGroup.Bicycle, "Велосипеды"),
            [28] = Create(28, TrueApiGroup.Vetpharma, "Ветеринарные препараты"),
            [29] = Create(29, TrueApiGroup.Petfood, "Корма для животных"),
            [30] = Create(30, TrueApiGroup.Vegetableoil, "Растительные масла"),
            [31] = Create(31, TrueApiGroup.Beer, "Слабоалкогольные напитки"),
            [32] = Create(32, TrueApiGroup.Conserve, "Консервированные продукты"),
            [33] = Create(33, TrueApiGroup.Grocery, "Бакалея"),
            [34] = Create(34, TrueApiGroup.Autofluids, "Моторные масла"),
            [35] = Create(35, TrueApiGroup.Bio, "Спортивное питание"),
            [36] = Create(36, TrueApiGroup.Toys, "Детские товары"),
            [37] = Create(37, TrueApiGroup.Chemistry, "Косметика, бытовая химия и товары личной гигиены"),
            [38] = Create(38, TrueApiGroup.Softdrinks, "Растворимые напитки"),
            [39] = Create(39, TrueApiGroup.Sweets, "Сладости"),
            [40] = Create(40, TrueApiGroup.Construction, "Стройматериалы"),
            [42] = Create(42, TrueApiGroup.Meat, "Мясные изделия")
        };

    /// <summary>
    /// ЕМЦ (smp) по умолчанию проверяется у табака (3) и никотиносодержащей продукции (16).
    /// </summary>
    public static bool DefaultCheckSmp(int trueApiGroupId)
        => trueApiGroupId == TrueApiGroup.Tobaco || trueApiGroupId == TrueApiGroup.Ncp;

    /// <summary>
    /// Создаёт запись маппинга с признаком ЕМЦ по коду Честного знака.
    /// </summary>
    public static GisMtProductMapping Create(int atolCode, int trueApiGroupId, string name)
        => new()
        {
            AtolCode = atolCode,
            TrueApiGroupId = trueApiGroupId,
            Name = name,
            CheckSmp = DefaultCheckSmp(trueApiGroupId)
        };

    /// <summary>
    /// Копия дефолтного маппинга для записи в конфигурацию.
    /// </summary>
    public static List<GisMtProductMapping> CopyDefaults()
        => Defaults.Values
            .Select(item => new GisMtProductMapping
            {
                AtolCode = item.AtolCode,
                TrueApiGroupId = item.TrueApiGroupId,
                Name = item.Name,
                CheckSmp = item.CheckSmp
            })
            .ToList();

    public static int? ToTrueApiGroup(int atolCode)
    {
        if (Defaults.TryGetValue(atolCode, out var mapping))
            return mapping.TrueApiGroupId;

        return null;
    }
}
