// js/config/menu.js
export const MENU_ITEMS = {
    MONITOR: {
        id: "monitorView",
        value: "Мониторинг"
    },
    CONFIG: {
        id: "config",
        value: "Настройка",
    },
    PRODUCT_GROUPS: {
        id: "productGroupsView",
        value: "Товарные группы"
    },
    CDN: {
        id: "cdnListInfo",
        value: "Список CDN"
    },
    BEER_TAPS: {
        id: "beerTapsView",
        value: "Пивные краны"
    },
    MARKS: {
        id: "marksView",
        value: "Марки"
    },
    GTIN_CATALOG: {
        id: "gtinCatalogView",
        value: "Каталог GTIN"
    },
    GIS_MT_MARKS: {
        id: "gisMtMarksView",
        value: "Остатки марок ГИС МТ"
    },
    MARK_CHECK: {
        id: "markCheckView",
        value: "Проверка маркировки"
    },
    LOGS: {
        id: "logsView",
        value: "Логи"
    },
    INFO: {
        id: "information",
        value: "Информация"
    },
};

export function buildMenuItems(config) {
    const items = [
        MENU_ITEMS.MONITOR,
        MENU_ITEMS.CONFIG,
        MENU_ITEMS.PRODUCT_GROUPS,
    ];

    const tsPiotEnabled = config?.serverConfig?.tsPiotEnabled ?? false;
    const useBeerTaps = config?.saleControlConfig?.useBeerTaps ?? false;

    if (!tsPiotEnabled) {
        items.push(MENU_ITEMS.CDN);
    }

    if (useBeerTaps) {
        items.push(MENU_ITEMS.BEER_TAPS);
    }

    items.push(
        MENU_ITEMS.MARKS,
        MENU_ITEMS.GTIN_CATALOG,
        MENU_ITEMS.GIS_MT_MARKS,
        MENU_ITEMS.MARK_CHECK,
        MENU_ITEMS.LOGS,
        MENU_ITEMS.INFO,
    );

    return items;
}