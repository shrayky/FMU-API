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
    CDN: {
        id: "cdnListInfo",
        value: "Список CDN"
    },
    MARKS: {
        id: "marksView",
        value: "Марки"
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

// Вспомогательная функция для получения плоского списка всех ID
export const getAllMenuIds = () => {
    const ids = [];
    Object.values(MENU_ITEMS).forEach(item => {
        ids.push(item.id);
        if (item.data) {
            item.data.forEach(subItem => ids.push(subItem.id));
        }
    });
    return ids;
};

// Вспомогательная функция для получения пути к элементу меню
export const getMenuPath = (id) => {
    for (const [section, item] of Object.entries(MENU_ITEMS)) {
        if (item.id === id) return [section];
        if (item.data) {
            const subItem = item.data.find(sub => sub.id === id);
            if (subItem) return [section, subItem.id];
        }
    }
    return null;
};

const subMenuData = {
    data: [
        {
            id: "serverConfigData",
            value: "Сервер"
        },
        {
            id: "loggingConfigData",
            value: "Логирование"
        },
        {
            id: "autoUpdateData",
            value: "Автоматическое обновление"
        },
        {
            id: "organisationsData",
            value: "Организации"
        },
        {
            id: "checkInternetConnectionHosts",
            value: "Проверка интернета"
        },
        {
            id: "couchDbData",
            value: "База данных"
        },
        {
            id: "frontolDbConnection",
            value: "Frontol"
        },
        {
            id: "frontolMarkUnit",
            value: "Frontol Markunit"
        },
        {
            id: "tokenServiceData",
            value: "Сервис получения токена ЧЗ"
        },
        {
            id: "timeoutConfig",
            value: "Таймауты"
        },
        {
            id: "salesControl",
            value: "Контроль продаж"
        },
        {
            id: "minimalPrices",
            value: "Минимальные цены"
        }
    ]
}