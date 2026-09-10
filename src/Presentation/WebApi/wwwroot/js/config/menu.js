// js/config/menu.js
export const MENU_ITEMS = {
    MONITOR: {
        id: "monitorView",
        value: "Мониторинг",
        icon: "mdi mdi-monitor-dashboard"
    },
    CONFIG: {
        id: "config",
        value: "Настройка",
        icon: "mdi mdi-cog"
    },
    PRODUCT_GROUPS: {
        id: "productGroupsView",
        value: "Товарные группы",
        icon: "mdi mdi-folder"
    },
    CDN: {
        id: "cdnListInfo",
        value: "Список CDN",
        icon: "mdi mdi-cloud"
    },
    BEER_TAPS: {
        id: "beerTapsView",
        value: "Пивные краны",
        icon: "mdi mdi-beer"
    },
    MARKS: {
        id: "marksView",
        value: "Марки",
        icon: "mdi mdi-barcode"
    },
    GTIN_CATALOG: {
        id: "gtinCatalogView",
        value: "Каталог GTIN",
        icon: "mdi mdi-table-chart"
    },
    GIS_MT_MARKS: {
        id: "gisMtMarksView",
        value: "Остатки марок ГИС МТ",
        icon: "mdi mdi-package-variant"
    },
    MARK_CHECK: {
        id: "markCheckView",
        value: "Проверка маркировки",
        icon: "mdi mdi-qrcode-scan"
    },
    LOGS: {
        id: "logsView",
        value: "Логи",
        icon: "mdi mdi-file-log"
    },
    INFO: {
        id: "information",
        value: "Информация",
        icon: "mdi mdi-information"
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