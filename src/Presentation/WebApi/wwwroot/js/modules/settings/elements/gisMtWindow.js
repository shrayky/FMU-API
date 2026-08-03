/**
 * Окно работы с ГИС МТ для выбранной организации.
 */
export function openGisMtWindow({ item, selectedId, labels, onSaveConfiguration }) {
    const inn = String(item.inn || "").trim();
    if (!inn) {
        webix.message({ text: "У организации не указан ИНН", type: "error" });
        return;
    }

    const windowId = "GisMtOrgWindow";
    if ($$(windowId))
        $$(windowId).destructor();

    const groups = (item.trueApiIntegrationSettings?.productGroups ?? []).map((g, i) => ({
        id: i + 1,
        name: g
    }));

    const WINDOW_WIDTH = 640;
    const WINDOW_HEIGHT = 480;
    const TAB_BODY_HEIGHT = 380;

    webix.ui({
        view: "window",
        id: windowId,
        position: "center",
        modal: true,
        width: WINDOW_WIDTH,
        height: WINDOW_HEIGHT,
        resize: false,
        head: {
            view: "toolbar",
            elements: [
                { view: "label", label: `${labels.gisMtWindowTitle}: ${item.name || inn}` },
                {
                    view: "icon",
                    icon: "wxi-close",
                    click: () => $$(windowId).close()
                }
            ]
        },
        body: {
            view: "tabview",
            id: "GisMtTabView",
            height: TAB_BODY_HEIGHT,
            cells: [
                {
                    header: labels.productGroupsTab,
                    body: {
                        id: "GisMtGroupsTab",
                        width: WINDOW_WIDTH - 20,
                        height: TAB_BODY_HEIGHT - 40,
                        padding: 10,
                        rows: [
                            {
                                cols: [
                                    {
                                        view: "button",
                                        value: labels.loadProductGroups,
                                        width: 180,
                                        click: () => loadProductGroups(inn, selectedId, onSaveConfiguration)
                                    },
                                    {}
                                ]
                            },
                            {
                                view: "datatable",
                                id: "GisMtProductGroupsTable",
                                columns: [
                                    { id: "name", header: "Товарная группа", fillspace: true }
                                ],
                                data: groups,
                                scroll: "y",
                                autoheight: false,
                                gravity: 1
                            }
                        ]
                    }
                },
                {
                    header: labels.operationsTab,
                    body: {
                        id: "GisMtOperationsTab",
                        width: WINDOW_WIDTH - 20,
                        height: TAB_BODY_HEIGHT - 40,
                        padding: 10,
                        rows: [
                            {
                                view: "button",
                                value: labels.syncDocuments,
                                width: 240,
                                click: async () => {
                                    try {
                                        const response = await fetch("/api/ts/gismt/sync", { method: "POST" });
                                        const data = await response.json().catch(() => ({}));
                                        if (!response.ok) {
                                            webix.message({
                                                text: data.error || "Ошибка загрузки документов",
                                                type: "error"
                                            });
                                            return;
                                        }
                                        webix.message(
                                            `Документов: ${data.documentsLoaded ?? 0}, марок: ${data.marksSaved ?? 0}`
                                        );
                                    } catch (e) {
                                        webix.message({
                                            text: e?.message || "Ошибка загрузки документов",
                                            type: "error"
                                        });
                                    }
                                }
                            },
                            {
                                view: "button",
                                value: labels.loadStock,
                                width: 240,
                                click: async () => {
                                    try {
                                        const response = await fetch(
                                            `/api/ts/gismt/stock/load?inn=${encodeURIComponent(inn)}`,
                                            { method: "POST" }
                                        );
                                        const data = await response.json().catch(() => ({}));
                                        if (!response.ok) {
                                            webix.message({
                                                text: data.error || "Ошибка загрузки остатков",
                                                type: "error"
                                            });
                                            return;
                                        }
                                        webix.message(`Загружено марок остатка: ${data.marksSaved ?? 0}`);
                                    } catch (e) {
                                        webix.message({
                                            text: e?.message || "Ошибка загрузки остатков",
                                            type: "error"
                                        });
                                    }
                                }
                            },
                            {}
                        ]
                    }
                }
            ]
        }
    }).show();
}

async function loadProductGroups(inn, selectedId, onSaveConfiguration) {
    try {
        const response = await fetch(
            `/api/ts/gismt/product-groups/refresh?inn=${encodeURIComponent(inn)}`,
            { method: "POST" }
        );
        const data = await response.json().catch(() => ({}));
        if (!response.ok) {
            webix.message({
                text: data.error || "Ошибка загрузки групп",
                type: "error"
            });
            return;
        }

        const productGroups = data.productGroups ?? [];
        const tableItem = $$("PrintGroups").getItem(selectedId);
        tableItem.trueApiIntegrationSettings = {
            ...(tableItem.trueApiIntegrationSettings ?? {}),
            enable: !!(tableItem.trueApiIntegrationSettings?.enable),
            productGroups
        };
        $$("PrintGroups").updateItem(selectedId, tableItem);

        const table = $$("GisMtProductGroupsTable");
        if (table) {
            table.clearAll();
            table.parse(productGroups.map((g, i) => ({ id: i + 1, name: g })));
        }

        onSaveConfiguration();
        webix.message("Товарные группы загружены");
    } catch (e) {
        webix.message({
            text: e?.message || "Ошибка загрузки групп",
            type: "error"
        });
    }
}
