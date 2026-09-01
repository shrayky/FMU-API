import { ApiServerAddress } from "../../utils/net.js";

const WINDOW_ID = "MarkingCalendarWindow";
const TABLE_ID = "markingCalendarTable";
const YEAR_ID = "markingCalendarYear";
const GROUP_ID = "markingCalendarGroup";
const EVENT_ID = "markingCalendarEvents";

const EVENT_RETAIL = "Розничная продажа";
const EVENT_PERMIT = "Разрешительный режим";
const ALLOWED_EVENTS = [EVENT_RETAIL, EVENT_PERMIT];
const ALL_VALUE = "all";
const ICON_BASE = "https://xn--80ajghhoc2aj1c8b.xn--p1ai";

const MONTHS = [
    { id: 1, header: "янв" },
    { id: 2, header: "фев" },
    { id: 3, header: "мар" },
    { id: 4, header: "апр" },
    { id: 5, header: "май" },
    { id: 6, header: "июн" },
    { id: 7, header: "июл" },
    { id: 8, header: "авг" },
    { id: 9, header: "сен" },
    { id: 10, header: "окт" },
    { id: 11, header: "ноя" },
    { id: 12, header: "дек" }
];

let cachedItems = null;
let stylesAdded = false;

const LABELS = {
    title: "Календарь внедрения в рознице",
    allGroups: "Все товарные группы",
    allEvents: "Все события",
    reset: "Сбросить",
    apply: "Применить",
    loadError: "Не удалось загрузить календарь внедрения",
    emptyYear: "Нет событий за выбранный год",
    loading: "Загружаю календарь..."
};

/**
 * Открывает окно календаря внедрения.
 */
export function openMarkingCalendarWindow() {
    ensureStyles();

    if ($$(WINDOW_ID))
        $$(WINDOW_ID).destructor();

    const currentYear = new Date().getFullYear();

    webix.ui({
        view: "window",
        id: WINDOW_ID,
        position: "center",
        modal: true,
        resize: true,
        width: Math.min(window.innerWidth * 0.95, 1400),
        height: Math.min(window.innerHeight * 0.9, 800),
        head: {
            view: "toolbar",
            elements: [
                { view: "label", label: LABELS.title },
                {
                    view: "icon",
                    icon: "wxi-close",
                    click: () => $$(WINDOW_ID).close()
                }
            ]
        },
        body: {
            padding: 12,
            rows: [
                {
                    cols: [
                        {
                            view: "richselect",
                            id: YEAR_ID,
                            width: 110,
                            value: String(currentYear),
                            options: yearOptions([currentYear]),
                            on: {
                                onChange: applyFilters
                            }
                        },
                        {
                            view: "combo",
                            id: GROUP_ID,
                            placeholder: LABELS.allGroups,
                            value: ALL_VALUE,
                            options: [{ id: ALL_VALUE, value: LABELS.allGroups }]
                        },
                        {
                            view: "richselect",
                            id: EVENT_ID,
                            width: 240,
                            value: ALL_VALUE,
                            options: [
                                { id: ALL_VALUE, value: LABELS.allEvents },
                                { id: EVENT_RETAIL, value: EVENT_RETAIL },
                                { id: EVENT_PERMIT, value: EVENT_PERMIT }
                            ]
                        },
                        {
                            view: "button",
                            value: LABELS.reset,
                            width: 110,
                            click: resetFilters
                        },
                        {
                            view: "button",
                            value: LABELS.apply,
                            width: 130,
                            css: "mc-apply-button",
                            click: applyFilters
                        }
                    ]
                },
                {
                    view: "template",
                    height: 36,
                    borderless: true,
                    css: "mc-legend",
                    template:
                        `<span class="mc-legend-item">
                            <span class="mc-legend-dot mc-block-permit"></span>Разрешительный режим
                        </span>
                        <span class="mc-legend-item">
                            <span class="mc-legend-dot mc-block-retail"></span>Розничная продажа
                        </span>`
                },
                {
                    view: "datatable",
                    id: TABLE_ID,
                    css: "mc-calendar",
                    rowHeight: 42,
                    scroll: "y",
                    columns: buildColumns(currentYear)
                }
            ]
        }
    }).show();

    loadAndRender();
}

/**
 * Подключает стили блоков календаря для тёмной темы Webix.
 */
function ensureStyles() {
    if (stylesAdded)
        return;

    webix.html.addStyle(`
        .mc-apply-button button {
            background: #e8b923 !important;
            color: #1a1a1a !important;
            font-weight: 600;
        }
        .mc-legend {
            display: flex;
            align-items: center;
            gap: 24px;
            padding-left: 4px;
        }
        .mc-legend-item {
            display: inline-flex;
            align-items: center;
            gap: 8px;
            color: #cfd3dc;
            font-size: 13px;
        }
        .mc-legend-dot {
            width: 12px;
            height: 12px;
            border-radius: 50%;
            display: inline-block;
        }
        .mc-calendar .webix_cell {
            padding: 4px 0;
        }
        .mc-group-cell {
            display: flex;
            align-items: center;
            gap: 10px;
            padding: 0 8px;
        }
        .mc-icon {
            width: 24px;
            height: 24px;
            border-radius: 50%;
            object-fit: cover;
            background: #3a3f4b;
            flex-shrink: 0;
        }
        .mc-group-name {
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
        }
        .mc-block {
            display: block;
            height: 16px;
            margin: 3px 8px;
            border-radius: 4px;
        }
        .mc-block-retail {
            background: #4ade80;
        }
        .mc-block-permit {
            background: #a78bfa;
        }
    `);

    stylesAdded = true;
}

/**
 * Строит колонки таблицы для выбранного года.
 */
function buildColumns(year) {
    const monthColumns = MONTHS.map(month => ({
        id: `m${month.id}`,
        header: { text: month.header, css: { "text-align": "center" } },
        fillspace: true,
        minWidth: 56,
        css: { "text-align": "center" },
        template: obj => monthCellHtml(obj.months[month.id] || [])
    }));

    return [
        {
            id: "name",
            header: String(year),
            width: 280,
            template: groupCellHtml
        },
        ...monthColumns
    ];
}

/**
 * HTML ячейки товарной группы.
 */
function groupCellHtml(obj) {
    const name = escapeHtml(obj.name);
    if (!obj.icon)
        return `<div class="mc-group-cell"><span class="mc-group-name">${name}</span></div>`;

    return `<div class="mc-group-cell">
        <img class="mc-icon" src="${escapeHtml(obj.icon)}" alt="" onerror="this.style.display='none'">
        <span class="mc-group-name">${name}</span>
    </div>`;
}

/**
 * HTML цветных блоков событий в месяце.
 */
function monthCellHtml(events) {
    if (!events.length)
        return "";

    return events.map(event => {
        const css = event.event === EVENT_PERMIT ? "mc-block-permit" : "mc-block-retail";
        return `<div class="mc-block ${css}" title="${escapeHtml(event.tooltip)}"></div>`;
    }).join("");
}

/**
 * Загружает данные с прокси и заполняет фильтры.
 */
async function loadAndRender() {
    const table = $$(TABLE_ID);
    if (!table)
        return;

    table.showOverlay(LABELS.loading);

    try {
        const items = await loadItems();
        fillFilterOptions(items);
        applyFilters();
    } catch (error) {
        table.hideOverlay();
        webix.message({
            type: "error",
            text: error.message || LABELS.loadError
        });
    }
}

/**
 * Загружает события календаря через API FMU.
 */
async function loadItems() {
    if (cachedItems)
        return cachedItems;

    const response = await fetch(ApiServerAddress("/marking-calendar"));
    if (!response.ok) {
        const text = await response.text();
        throw new Error(text || LABELS.loadError);
    }

    const packet = await response.json();
    const rawItems = packet?.data?.items;
    if (packet?.status !== "success" || !Array.isArray(rawItems))
        throw new Error(LABELS.loadError);

    cachedItems = rawItems
        .filter(item => ALLOWED_EVENTS.includes(item.event))
        .map(normalizeItem)
        .filter(item => item.date);

    return cachedItems;
}

/**
 * Приводит элемент API к виду для таблицы.
 */
function normalizeItem(item) {
    const name = decodeHtml(item.tg_name);
    const date = parseRuDate(item.date_start);
    const iconPath = item.tg_icon_path || "";

    return {
        name,
        event: item.event,
        stage: decodeHtml(item.stage),
        date,
        icon: iconPath ? `${ICON_BASE}${iconPath}` : "",
        tooltip: [item.date_start, item.event, decodeHtml(item.stage)].filter(Boolean).join(" — ")
    };
}

/**
 * Заполняет списки года и товарных групп.
 */
function fillFilterOptions(items) {
    const years = [...new Set(items.map(item => item.date.year))].sort((a, b) => b - a);
    const currentYear = new Date().getFullYear();
    if (!years.includes(currentYear))
        years.unshift(currentYear);

    const groups = [...new Map(items.map(item => [item.name, item.name])).values()]
        .sort((a, b) => a.localeCompare(b, "ru"));

    $$(YEAR_ID).define("options", yearOptions(years));
    $$(YEAR_ID).setValue(String(currentYear));
    $$(YEAR_ID).refresh();

    $$(GROUP_ID).define("options", [
        { id: ALL_VALUE, value: LABELS.allGroups },
        ...groups.map(name => ({ id: name, value: name }))
    ]);
    $$(GROUP_ID).setValue(ALL_VALUE);
    $$(GROUP_ID).refresh();
}

/**
 * Сбрасывает фильтры к значениям по умолчанию.
 */
function resetFilters() {
    $$(YEAR_ID).setValue(String(new Date().getFullYear()));
    $$(GROUP_ID).setValue(ALL_VALUE);
    $$(EVENT_ID).setValue(ALL_VALUE);
    applyFilters();
}

/**
 * Применяет выбранные фильтры к таблице.
 */
function applyFilters() {
    const table = $$(TABLE_ID);
    if (!table || !cachedItems)
        return;

    const year = Number($$(YEAR_ID).getValue());
    const group = $$(GROUP_ID).getValue();
    const eventName = $$(EVENT_ID).getValue();
    const rows = buildRows(cachedItems, year, group, eventName);

    table.define("columns", buildColumns(year));
    table.refreshColumns();
    table.clearAll();
    table.parse(rows);

    if (rows.length === 0)
        table.showOverlay(LABELS.emptyYear);
    else
        table.hideOverlay();
}

/**
 * Собирает строки календаря по фильтрам.
 */
function buildRows(items, year, group, eventName) {
    const rowsByName = new Map();

    for (const item of items) {
        if (item.date.year !== year)
            continue;
        if (group !== ALL_VALUE && item.name !== group)
            continue;
        if (eventName !== ALL_VALUE && item.event !== eventName)
            continue;

        if (!rowsByName.has(item.name)) {
            rowsByName.set(item.name, {
                id: item.name,
                name: item.name,
                icon: item.icon,
                months: {}
            });
        }

        const row = rowsByName.get(item.name);
        const monthEvents = row.months[item.date.month] || [];
        monthEvents.push(item);
        row.months[item.date.month] = monthEvents;
    }

    return [...rowsByName.values()];
}

/**
 * Годы для richselect: Webix не умеет options из чисел.
 */
function yearOptions(years) {
    return years.map(year => {
        const value = String(year);
        return { id: value, value };
    });
}

/**
 * Разбирает дату в формате ДД.ММ.ГГГГ.
 */
function parseRuDate(value) {
    if (!value)
        return null;

    const parts = String(value).split(".");
    if (parts.length < 3)
        return null;

    const month = Number(parts[1]);
    const year = Number(parts[2]);
    if (!month || !year)
        return null;

    return { month, year };
}

/**
 * Декодирует HTML-сущности из ответа Честного знака.
 */
function decodeHtml(value) {
    const textarea = document.createElement("textarea");
    textarea.innerHTML = value ?? "";
    return textarea.value;
}

/**
 * Экранирует текст для вставки в HTML.
 */
function escapeHtml(value) {
    return webix.template.escape(String(value ?? ""));
}
