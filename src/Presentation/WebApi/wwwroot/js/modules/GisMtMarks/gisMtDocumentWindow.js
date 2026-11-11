const DOCUMENTS_URL = "/api/ts/gismt/documents";
const WINDOW_ID = "gisMtDocumentWindow";
const INFO_ID = "gisMtDocumentInfo";
const BODY_ID = "gisMtDocumentBody";
const TABLE_ID = "gisMtDocumentMarks";
const TITLE_ID = "gisMtDocumentTitle";
const RELOAD_ID = "gisMtDocumentReload";

const escape = (value) => webix.template.escape(String(value ?? ""));

const formatDate = (value) => (value ? new Date(value).toLocaleDateString() : "");

const formatMoment = (value) => (value ? new Date(value).toLocaleString() : "");

const prettyJson = (raw) => {
    if (!raw)
        return "";

    try {
        return JSON.stringify(JSON.parse(raw), null, 2);
    } catch {
        return String(raw);
    }
};

export default function openGisMtDocument(number) {
    const existing = $$(WINDOW_ID);

    if (existing)
        existing.destructor();

    let currentDocument = null;
    let marks = [];

    const infoHtml = () => {
        if (!currentDocument)
            return "";

        const document = currentDocument;
        const rows = [
            ["Номер", document.number],
            ["Дата документа", formatDate(document.docDate)],
            ["Тип", document.type],
            ["Статус", document.status],
            ["Отправитель, ИНН", document.senderInn],
            ["Получатель, ИНН", document.receiverInn],
            ["Организация, ИНН", document.organisationInn],
            ["Товарная группа", document.productGroup],
            ["Марок", marks.length === document.marksCount
                ? String(document.marksCount ?? 0)
                : `${marks.length} из ${document.marksCount}`],
            ["Загружен", formatMoment(document.loadedAt)]
        ].filter(([, value]) => String(value ?? "").trim().length > 0);

        return `<div style="font-size:13px;line-height:1.4">${rows
            .map(([label, value]) => `<div style="margin:2px 0"><span style="color:#888">${escape(label)}:</span> ${escape(value)}</div>`)
            .join("")}</div>`;
    };

    const bodyHtml = () => {
        const text = prettyJson(currentDocument?.body);

        if (!text)
            return '<div style="color:#888">Тело документа не сохранено — нажмите «Перезагрузить».</div>';

        return `<pre style="margin:0;font-size:12px;line-height:1.35;white-space:pre-wrap;word-break:break-word">${escape(text)}</pre>`;
    };

    const marksRows = () => marks.map((mark, index) => ({
        id: index + 1,
        sgtin: mark.sGtin || mark.id,
        cis: mark.cis,
        status: mark.status,
        circulation: mark.sold ? "Нет" : "Да",
        expireDate: formatDate(mark.expireDate),
        ownerInn: mark.ownerInn
    }));

    const load = async () => {
        try {
            const response = await fetch(`${DOCUMENTS_URL}/by-number?number=${encodeURIComponent(number)}`);
            const body = await response.json().catch(() => ({}));

            if (!response.ok) {
                webix.message({ text: body?.error || "Документ не открыт", type: "error", expire: 8000 });
                return;
            }

            currentDocument = body.document;
            marks = body.marks || [];

            const title = $$(TITLE_ID);
            if (title && currentDocument) {
                const date = formatDate(currentDocument.docDate);
                title.setValue(date ? `Документ ${currentDocument.number} от ${date}` : `Документ ${currentDocument.number}`);
            }

            const info = $$(INFO_ID);
            if (info)
                info.refresh();

            const bodyView = $$(BODY_ID);
            if (bodyView)
                bodyView.refresh();

            const table = $$(TABLE_ID);
            if (table) {
                table.clearAll();
                table.parse(marksRows());
            }
        } catch (error) {
            webix.message({ text: error?.message || "Документ не открыт", type: "error" });
        }
    };

    const reload = async () => {
        const button = $$(RELOAD_ID);

        if (button)
            button.disable();

        try {
            const response = await fetch(`${DOCUMENTS_URL}/reload?number=${encodeURIComponent(number)}`, { method: "POST" });
            const body = await response.json().catch(() => ({}));

            if (!response.ok) {
                webix.message({ text: body?.error || "Документ не перезагружен", type: "error", expire: 8000 });
                return;
            }

            webix.message(`Марок в документе: ${body.marks ?? 0}`);
            await load();
        } catch (error) {
            webix.message({ text: error?.message || "Документ не перезагружен", type: "error" });
        } finally {
            if (button)
                button.enable();
        }
    };

    webix.ui({
        view: "window",
        id: WINDOW_ID,
        position: "center",
        modal: true,
        move: false,
        resize: true,
        width: Math.min(window.innerWidth * 0.9, 1100),
        height: Math.min(window.innerHeight * 0.9, 640),
        head: {
            view: "toolbar",
            elements: [
                { view: "label", id: TITLE_ID, label: "Документ ГИС МТ" },
                { view: "icon", icon: "wxi-close", click: () => $$(WINDOW_ID).close() }
            ]
        },
        body: {
            padding: 10,
            rows: [
                {
                    view: "tabview",
                    cells: [
                        {
                            header: "Сведения",
                            body: {
                                rows: [
                                    {
                                        view: "template",
                                        id: INFO_ID,
                                        borderless: true,
                                        autoheight: true,
                                        template: () => infoHtml()
                                    },
                                    {
                                        view: "datatable",
                                        id: TABLE_ID,
                                        columns: [
                                            { id: "sgtin", header: "sGTIN", fillspace: 2, template: obj => escape(obj.sgtin) },
                                            { id: "cis", header: "КИ", fillspace: 2, template: obj => escape(obj.cis) },
                                            { id: "status", header: "Статус", width: 130, template: obj => escape(obj.status) },
                                            { id: "circulation", header: "В обороте", width: 110, template: obj => escape(obj.circulation) },
                                            { id: "expireDate", header: "Срок годности", width: 130, template: obj => escape(obj.expireDate) },
                                            { id: "ownerInn", header: "ИНН владельца", width: 130, template: obj => escape(obj.ownerInn) }
                                        ],
                                        scroll: "y",
                                        data: []
                                    }
                                ]
                            }
                        },
                        {
                            header: "Тело документа",
                            body: {
                                view: "scrollview",
                                scroll: "y",
                                body: {
                                    view: "template",
                                    id: BODY_ID,
                                    borderless: true,
                                    template: () => bodyHtml()
                                }
                            }
                        }
                    ]
                },
                {
                    cols: [
                        { view: "button", id: RELOAD_ID, value: "Перезагрузить", width: 170, click: () => reload() },
                        { view: "button", value: "Закрыть", width: 140, click: () => $$(WINDOW_ID).close() },
                        {}
                    ]
                }
            ]
        }
    }).show();

    load();
}
