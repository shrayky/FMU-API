const WINDOW_ID = "gisMtModsWindow";
const MODS_URL = "/api/ts/gismt/mods";

export default function openGisMtModsWindow({ inn, onSelect }) {
    const existing = $$(WINDOW_ID);

    if (existing)
        existing.destructor();

    let places = [];

    const applyFilter = () => {
        const term = String($$("gisMtModsSearch").getValue() || "").trim().toLowerCase();
        const table = $$("gisMtModsTable");

        if (!table)
            return;

        const rows = places
            .filter(place => {
                if (!term)
                    return true;

                return `${place.address} ${place.fiasId} ${place.kpp}`.toLowerCase().includes(term);
            })
            .map((place, index) => ({ id: index + 1, ...place }));

        table.clearAll();
        table.parse(rows);
    };

    const choose = () => {
        const table = $$("gisMtModsTable");
        const selected = table ? table.getSelectedItem() : null;

        if (!selected)
            return;

        onSelect({ fiasId: selected.fiasId ?? "", kpp: selected.kpp ?? "" });
        $$(WINDOW_ID).close();
    };

    webix.ui({
        view: "window",
        id: WINDOW_ID,
        position: "center",
        modal: true,
        move: false,
        width: Math.min(window.innerWidth * 0.8, 900),
        height: 520,
        head: {
            view: "toolbar",
            elements: [
                { view: "label", label: "Места осуществления деятельности" },
                { view: "icon", icon: "wxi-close", click: () => $$(WINDOW_ID).close() }
            ]
        },
        body: {
            padding: 10,
            rows: [
                {
                    view: "text",
                    id: "gisMtModsSearch",
                    placeholder: "Поиск по адресу, ФИАС или КПП",
                    on: {
                        onTimedKeyPress: () => applyFilter()
                    }
                },
                {
                    view: "datatable",
                    id: "gisMtModsTable",
                    columns: [
                        { id: "address", header: "Адрес", fillspace: 3, template: obj => webix.template.escape(obj.address ?? "") },
                        { id: "fiasId", header: "ФИАС", fillspace: 1.5, template: obj => webix.template.escape(obj.fiasId ?? "") },
                        { id: "kpp", header: "КПП", width: 140, template: obj => webix.template.escape(obj.kpp ?? "") }
                    ],
                    select: "row",
                    scroll: "y",
                    data: []
                },
                {
                    cols: [
                        { view: "button", value: "Выбрать", width: 160, click: () => choose() },
                        { view: "button", value: "Обновить", width: 160, click: () => load() },
                        { view: "button", value: "Закрыть", width: 140, click: () => $$(WINDOW_ID).close() },
                        {}
                    ]
                }
            ]
        }
    }).show();

    const load = async () => {
        try {
            const response = await fetch(`${MODS_URL}?inn=${encodeURIComponent(inn)}`);
            const body = await response.json();

            if (!response.ok) {
                webix.message({ text: body?.error || "Места не загружены", type: "error", expire: 8000 });
                return;
            }

            places = body;
            applyFilter();
        } catch (error) {
            webix.message({ text: error?.message || "Места не загружены", type: "error" });
        }
    };

    load();
}
