import { ApiServerAddress } from "../../utils/net.js";

const WINDOW_ID = "CrptEspCheckWindow";
const TABLE_ID = "crptEspCheckTable";
const SUMMARY_ID = "crptEspCheckSummary";
const RUN_BUTTON_ID = "crptEspCheckRun";

let stylesAdded = false;

const LABELS = {
    title: "Проверка ЦРПТ / АО ЕСП",
    hint: "Проверка выполняется с сервера FMU-API",
    run: "Проверить",
    checking: "Проверка...",
    loadError: "Не удалось выполнить проверку адресов",
    available: "ДОСТУПЕН",
    unavailable: "НЕДОСТУПЕН"
};

export function openCrptEspCheckWindow() {
    ensureStyles();

    if ($$(WINDOW_ID))
        $$(WINDOW_ID).destructor();

    webix.ui({
        view: "window",
        id: WINDOW_ID,
        position: "center",
        modal: true,
        resize: true,
        width: Math.min(window.innerWidth * 0.9, 1100),
        height: Math.min(window.innerHeight * 0.9, 720),
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
                            view: "template",
                            id: SUMMARY_ID,
                            borderless: true,
                            height: 42,
                            css: "crpt-esp-summary",
                            template: LABELS.hint
                        },
                        {
                            view: "button",
                            id: RUN_BUTTON_ID,
                            value: LABELS.run,
                            width: 140,
                            css: "crpt-esp-run-button",
                            click: runCheck
                        }
                    ]
                },
                {
                    view: "datatable",
                    id: TABLE_ID,
                    css: "crpt-esp-table",
                    rowHeight: 36,
                    scroll: "y",
                    columns: [
                        { id: "index", header: "№", width: 50 },
                        { id: "group", header: "Группа", width: 120 },
                        { id: "address", header: "Адрес", fillspace: true },
                        {
                            id: "result",
                            header: "Результат",
                            width: 180,
                            template: resultCellHtml
                        },
                        { id: "elapsed", header: "Время", width: 100 }
                    ]
                }
            ]
        }
    }).show();

    loadHosts();
}

function ensureStyles() {
    if (stylesAdded)
        return;

    webix.html.addStyle(`
        .crpt-esp-run-button button {
            background: #e8b923 !important;
            color: #1a1a1a !important;
            font-weight: 600;
        }
        .crpt-esp-summary {
            display: flex;
            align-items: center;
            color: #cfd3dc;
            font-size: 14px;
        }
        .crpt-esp-ok {
            color: #4ade80;
            font-weight: 600;
        }
        .crpt-esp-error {
            color: #f87171;
            font-weight: 600;
        }
        .crpt-esp-checking {
            color: #9ca3af;
        }
    `);

    stylesAdded = true;
}

function resultCellHtml(obj) {
    if (obj.checking)
        return `<span class="crpt-esp-checking">${LABELS.checking}</span>`;

    if (obj.available)
        return `<span class="crpt-esp-ok">${LABELS.available}</span>`;

    if (obj.available === false)
        return `<span class="crpt-esp-error">${LABELS.unavailable}</span>`;

    return "—";
}

async function loadHosts() {
    const table = $$(TABLE_ID);
    if (!table)
        return;

    try {
        const packet = await requestJson("GET");
        fillTable(packet.items);
        $$(SUMMARY_ID)?.define("template", LABELS.hint);
        $$(SUMMARY_ID)?.refresh();
    } catch (error) {
        webix.message({
            type: "error",
            text: error.message || LABELS.loadError
        });
    }
}

async function runCheck() {
    const table = $$(TABLE_ID);
    const summary = $$(SUMMARY_ID);
    const button = $$(RUN_BUTTON_ID);

    if (!table)
        return;

    table.showOverlay(LABELS.checking);
    button?.disable();

    try {
        const packet = await requestJson("POST");
        fillTable(packet.items);
        table.hideOverlay();
        summary.define("template", summaryHtml(packet));
        summary.refresh();
    } catch (error) {
        table.hideOverlay();
        webix.message({
            type: "error",
            text: error.message || LABELS.loadError
        });
    } finally {
        button?.enable();
    }
}

async function requestJson(method) {
    const response = await fetch(ApiServerAddress("/crpt-esp-check"), { method });
    if (!response.ok) {
        const text = await response.text();
        throw new Error(text || LABELS.loadError);
    }

    const packet = await response.json();
    if (!Array.isArray(packet?.items))
        throw new Error(LABELS.loadError);

    return packet;
}

function fillTable(items) {
    const table = $$(TABLE_ID);
    table.clearAll();
    table.parse(items.map(toRow));
}

function toRow(item) {
    return {
        id: item.index,
        index: item.index,
        group: item.group,
        address: item.address,
        available: item.available,
        elapsed: item.elapsedMs == null ? "—" : `${item.elapsedMs} мс`
    };
}

function summaryHtml(packet) {
    return `${LABELS.hint}. `
        + `Доступно: <b>${packet.available}</b>&nbsp;&nbsp;`
        + `Недоступно: <b>${packet.unavailable}</b>&nbsp;&nbsp;`
        + `Всего: <b>${packet.total}</b>`;
}
