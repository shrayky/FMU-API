/**
 * Русские названия товарных групп ГИС МТ по справочнику маппинга.
 */
const MAPPING_API = "/api/product-groups/mapping";

/** Код ГИС МТ (pg) → код товарной группы Честного знака. */
const GIS_MT_CODE_TO_GROUP_ID = {
    lp: 1,
    shoes: 2,
    tobaco: 3,
    tobacco: 3,
    perfumery: 4,
    tires: 5,
    electronics: 6,
    pharma: 7,
    milk: 8,
    bicycle: 9,
    wheelchairs: 10,
    alcohol: 11,
    otp: 12,
    water: 13,
    furs: 14,
    beer: 15,
    ncp: 16,
    bio: 17,
    antiseptic: 19,
    petfood: 20,
    seafood: 21,
    nabeer: 22,
    softdrinks: 23,
    meat: 25,
    vetpharma: 26,
    toys: 27,
    radio: 28,
    titan: 31,
    conserve: 32,
    vegetableoil: 33,
    opticfiber: 34,
    chemistry: 35,
    books: 36,
    grocery: 37,
    pharmaraw: 38,
    construction: 39,
    fire: 40,
    heater: 41,
    cableraw: 42,
    autofluids: 43,
    polymer: 44,
    sweets: 45,
    carparts: 48,
    furslp: 49,
    nicotindev: 50,
    gadgets: 51,
    frozen: 52,
    fertilizers: 53,
    homeware: 54,
    pyrotechnics: 59
};

/** Русские названия по коду ЧЗ, если в справочнике нет своей строки. */
const FALLBACK_NAMES = {
    1: "Товары легкой промышленности",
    2: "Обувь",
    3: "Табачная продукция",
    4: "Парфюмерная продукция",
    5: "Шины",
    6: "Фототовары",
    7: "Лекарственные препараты",
    8: "Молочная продукция",
    9: "Велосипеды",
    10: "Медицинские изделия",
    11: "Алкогольная продукция",
    12: "Альтернативная табачная продукция",
    13: "Вода",
    14: "Изделия из меха",
    15: "Фасованное пиво",
    16: "Никотиносодержащая продукция",
    17: "БАДы",
    19: "Антисептики",
    20: "Корма для животных",
    21: "Икра осетровых и лососевых рыб",
    22: "Безалкогольное пиво",
    23: "Безалкогольные напитки",
    25: "Мясные изделия",
    26: "Ветеринарные препараты",
    27: "Детские товары",
    28: "Радиоэлектронная продукция",
    31: "Титановая металлопродукция",
    32: "Консервированные продукты",
    33: "Растительные масла",
    34: "Оптоволокно",
    35: "Косметика, бытовая химия и товары личной гигиены",
    36: "Печатная продукция",
    37: "Бакалея",
    38: "Лекарственные препараты",
    39: "Стройматериалы",
    40: "Средства пожаротушения",
    41: "Отопительные приборы",
    42: "Кабельно-проводниковая продукция",
    43: "Моторные масла",
    44: "Полимерные трубы",
    45: "Сладости",
    48: "Автозапчасти",
    49: "Меховые изделия легкой промышленности",
    50: "Никотиносодержащие устройства",
    51: "Электронные устройства",
    52: "Замороженная продукция",
    53: "Удобрения",
    54: "Товары для дома",
    59: "Пиротехнические изделия"
};

let namesByCode = {};
let namesByGroupId = {};
let loaded = false;
let loadPromise = null;

rebuildFromMapping([]);

/**
 * Загружает справочник товарных групп и подставляет русские названия поверх запасных.
 */
export async function loadProductGroupLabels() {
    if (loaded)
        return;

    if (loadPromise)
        return loadPromise;

    loadPromise = fetchAndApply();
    try {
        await loadPromise;
    } finally {
        loadPromise = null;
    }
}

export function productGroupLabel(code, groupId) {
    const key = String(code || "").trim().toLowerCase();
    if (key && namesByCode[key])
        return namesByCode[key];

    const id = Number(groupId);
    if (id && namesByGroupId[id])
        return namesByGroupId[id];

    if (key)
        return String(code).trim();

    return "";
}

async function fetchAndApply() {
    try {
        const response = await fetch(MAPPING_API);
        if (response.ok) {
            const rows = await response.json();
            rebuildFromMapping(Array.isArray(rows) ? rows : []);
        }
    } catch (error) {
        console.error("Ошибка загрузки названий товарных групп:", error);
    }

    loaded = true;
}

function rebuildFromMapping(rows) {
    namesByGroupId = { ...FALLBACK_NAMES };

    const sorted = [...rows].sort((a, b) => (Number(a.atolCode) || 0) - (Number(b.atolCode) || 0));
    const used = new Set();

    for (const row of sorted) {
        const groupId = Number(row.trueApiGroupId);
        const name = String(row.name || "").trim();
        if (!groupId || !name || used.has(groupId))
            continue;

        used.add(groupId);
        namesByGroupId[groupId] = name;
    }

    namesByCode = {};
    for (const [code, id] of Object.entries(GIS_MT_CODE_TO_GROUP_ID))
        namesByCode[code] = namesByGroupId[id] || code;
}
