import { productGroupLabel } from '../../utils/productGroupLabels.js';

const JOURNAL_URL = "/api/ts/gismt/journal";
const WINDOW_ID = "gisMtJournalDocumentWindow";

const KIND_LABELS = {
    CONNECT_TAP: "Постановка на кран",
    LK_RECEIPT: "Списание молочной продукции"
};

const REASON_LABELS = {
    PACKING: "Списание (фасовка)",
    EXPIRATION: "Списание (срок годности)"
};

const CRPT_STATUS_LABELS = {
    IN_PROGRESS: "Проверяется",
    CHECKED_OK: "Обработан",
    CHECKED_NOT_OK: "Обработан с ошибками",
    PROCESSING_ERROR: "Техническая ошибка",
    PARSE_ERROR: "Обработан с ошибками",
    ACCEPTED: "Принят",
    WAIT_ACCEPTANCE: "Ожидает приёмку"
};

const STATUS_LABELS = {
    draft: "Черновик",
    sending: "Отправляется",
    sent: "Отправлен",
    error: "Ошибка"
};

const escape = (value) => webix.template.escape(String(value ?? ""));

export default function openGisMtJournalDocument({ id, onChanged, onEdit }) {
    const existing = $$(WINDOW_ID);

    if (existing)
        existing.destructor();

    let currentDocument = null;

    const isReceipt = (document) => document.kind === "LK_RECEIPT";
    const isExpiration = (document) => document.reason === "EXPIRATION";
    const isLocked = (document) => document.status === "sent" || document.status === "sending";
    const isUnconfirmed = (document) => document.status === "sending"
        && !String(document.crptDocumentId ?? "").trim();
    const canEdit = (document) => document.kind === "LK_RECEIPT" && !isLocked(document);

    const kindLabel = (document) => isReceipt(document)
        ? (REASON_LABELS[isExpiration(document) ? "EXPIRATION" : "PACKING"])
        : (KIND_LABELS[document.kind] ?? document.kind);

    const crptStatusText = (document) => {
        const code = String(document.crptStatus ?? "");
        const errors = String(document.crptStatusText ?? "").trim();

        if (!code && !errors)
            return "";

        const label = CRPT_STATUS_LABELS[code] ?? code;

        return errors.length > 0 ? `${label}. ${errors}` : label;
    };

    const crptStatusColor = (code) => {
        switch (String(code ?? "").toUpperCase()) {
            case "CHECKED_OK":
            case "ACCEPTED":
                return "#2fbf71";
            case "CHECKED_NOT_OK":
            case "PARSE_ERROR":
            case "PROCESSING_ERROR":
                return "#dc3545";
            case "IN_PROGRESS":
            case "WAIT_ACCEPTANCE":
                return "#e0a030";
            default:
                return "";
        }
    };

    const headerHtml = (document) => {
        const rows = [
            ["Вид", kindLabel(document)],
            ["Товарная группа", productGroupLabel(document.productGroup)],
            ["Статус", STATUS_LABELS[document.status] ?? document.status],
            ["Дата документа", document.docDate ?? ""],
            ["ИНН", document.inn],
            document.fiasId ? ["ФИАС", document.fiasId] : ["КПП", document.kpp],
            ["Дата создания", document.createdAt ? new Date(document.createdAt).toLocaleString() : ""],
            document.sentAt ? ["Дата отправки", new Date(document.sentAt).toLocaleString()] : null,
            document.crptDocumentId ? ["Документ Честного знака", document.crptDocumentId] : null,
            document.uid ? ["uid Фронтола", document.uid] : null
        ].filter(Boolean);

        const lines = rows
            .map(([label, value]) => `<div style="margin:2px 0"><span style="color:#888">${escape(label)}:</span> ${escape(value)}</div>`)
            .join("");

        const statusText = crptStatusText(document);
        const color = crptStatusColor(document.crptStatus);
        const statusHtml = statusText.length > 0
            ? `<div style="margin:2px 0"><span style="color:#888">Статус ЧЗ:</span> <span style="color:${color || "#ddd"}">${escape(statusText)}</span></div>`
            : "";

        const error = String(document.errorText ?? "").trim();
        const errorHtml = error.length > 0
            ? `<div style="margin-top:6px;color:#dc3545">${escape(error)}</div>`
            : "";

        const detail = String(document.crptErrorsDetail ?? "").trim();
        const detailHtml = detail.length > 0
            ? `<div style="margin-top:6px;color:#d9a441">${escape(detail)}</div>`
            : "";

        return `<div style="font-size:13px;line-height:1.4">${lines}${statusHtml}${errorHtml}${detailHtml}</div>`;
    };

    const linesColumns = (document) => {
        if (isReceipt(document)) {
            const columns = [
                { id: "mark", header: [{ text: "Марка" }, { content: "textFilter" }], fillspace: 2, template: obj => escape(obj.mark) },
                { id: "productName", header: "Название", fillspace: 2, template: obj => escape(obj.productName) },
                { id: "cis", header: [{ text: "КИ" }, { content: "textFilter" }], fillspace: 2, template: obj => escape(obj.cis) },
                { id: "gtin", header: "GTIN", width: 150, template: obj => escape(obj.gtin) }
            ];

            if (!isExpiration(document)) {
                columns.push(
                    { id: "quantity", header: "Кол-во, кг", width: 100 },
                    { id: "price", header: "Цена, ₽/кг", width: 110 },
                    { id: "amount", header: "Сумма, ₽", width: 110 });
            }

            return columns;
        }

        return [
            { id: "cis", header: [{ text: "КИ" }, { content: "textFilter" }], fillspace: 3, template: obj => escape(obj.cis) },
            { id: "connectDate", header: "Дата подключения", width: 160, template: obj => escape(obj.connectDate) }
        ];
    };

    const linesRows = (document) => (document.lines || []).map((line, index) => ({
        id: index + 1,
        mark: line.mark ?? "",
        productName: line.productName ?? "",
        cis: line.cis ?? "",
        gtin: line.gtin ?? "",
        quantity: line.quantityKg == null ? "" : Number(line.quantityKg).toFixed(3),
        price: line.pricePerKg == null ? "" : Number(line.pricePerKg).toFixed(2),
        amount: line.amountKop == null ? "" : (line.amountKop / 100).toFixed(2),
        connectDate: line.connectDate ?? ""
    }));

    const sendDocument = async (documentId) => {
        try {
            const response = await fetch(`${JOURNAL_URL}/${encodeURIComponent(documentId)}/send`, { method: "POST" });
            const body = await response.json();

            if (!response.ok) {
                webix.message({ text: body?.error || "Документ не отправлен", type: "error", expire: 8000 });
                return;
            }

            webix.message("Документ отправлен");
            $$(WINDOW_ID).close();
            onChanged();
        } catch (error) {
            webix.message({ text: error?.message || "Документ не отправлен", type: "error" });
        }
    };

    const checkStatus = async (documentId) => {
        try {
            const response = await fetch(`${JOURNAL_URL}/${encodeURIComponent(documentId)}/status`, { method: "POST" });
            const body = await response.json();

            if (!response.ok) {
                webix.message({ text: body?.error || "Статус в Честном знаке не получен", type: "error", expire: 8000 });
                return;
            }

            const errors = String(body?.errors ?? "").trim();
            const text = errors.length > 0
                ? `Статус в ЧЗ: ${body?.statusText}. ${errors}`
                : `Статус в ЧЗ: ${body?.statusText}`;

            webix.message({ text, type: errors.length > 0 ? "error" : "info", expire: errors.length > 0 ? 12000 : 6000 });

            await load();
            onChanged();
        } catch (error) {
            webix.message({ text: error?.message || "Статус в Честном знаке не получен", type: "error" });
        }
    };

    const reconcileDocument = async (documentId) => {
        let crptDocumentId;

        try {
            crptDocumentId = await webix.prompt({
                title: "Документ найден в Честном знаке",
                text: "Укажите идентификатор документа из личного кабинета Честного знака:",
                input: { placeholder: "Идентификатор документа" },
                ok: "Сверить",
                cancel: "Отмена"
            });
        } catch {
            return;
        }

        const value = String(crptDocumentId ?? "").trim();

        if (!value) {
            webix.message({ text: "Идентификатор документа не указан", type: "warning", expire: 6000 });
            return;
        }

        try {
            const response = await fetch(`${JOURNAL_URL}/${encodeURIComponent(documentId)}/reconcile`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ crptDocumentId: value })
            });
            const body = await response.json();

            if (!response.ok) {
                webix.message({ text: body?.error || "Документ не сверен", type: "error", expire: 12000 });
                return;
            }

            webix.message("Акт сверен с документом Честного знака");
            await load();
            onChanged();
        } catch (error) {
            webix.message({ text: error?.message || "Документ не сверен", type: "error" });
        }
    };

    const markNotFound = async (documentId) => {
        let confirmed;

        try {
            confirmed = await webix.confirm({
                title: "Документа нет в Честном знаке",
                text: "Вернуть акт в ошибку? Отправку можно будет повторить.",
                ok: "Вернуть в ошибку",
                cancel: "Отмена"
            });
        } catch {
            return;
        }

        if (!confirmed)
            return;

        try {
            const response = await fetch(`${JOURNAL_URL}/${encodeURIComponent(documentId)}/unconfirmed/not-found`, { method: "POST" });
            const body = await response.json();

            if (!response.ok) {
                webix.message({ text: body?.error || "Акт не возвращён в ошибку", type: "error", expire: 12000 });
                return;
            }

            webix.message("Акт возвращён в ошибку, отправку можно повторить");
            await load();
            onChanged();
        } catch (error) {
            webix.message({ text: error?.message || "Акт не возвращён в ошибку", type: "error" });
        }
    };

    const load = async () => {
        try {
            const response = await fetch(`${JOURNAL_URL}/${encodeURIComponent(id)}`);
            const document = await response.json();

            if (!response.ok) {
                webix.message({ text: document?.error || "Документ не открыт", type: "error", expire: 8000 });
                return;
            }

            const body = $$("gisMtJournalDocumentBody");
            const table = $$("gisMtJournalDocumentLines");
            const sendButton = $$("gisMtJournalDocumentSend");
            const editButton = $$("gisMtJournalDocumentEdit");

            currentDocument = document;

            if (body)
                body.refresh();

            if (table) {
                table.config.columns = linesColumns(document);
                table.clearAll();
                table.refreshColumns();
                table.parse(linesRows(document));
            }

            if (sendButton) {
                if (isLocked(document))
                    sendButton.disable();
                else
                    sendButton.enable();
            }

            if (editButton) {
                if (canEdit(document))
                    editButton.show();
                else
                    editButton.hide();
            }

            const statusButton = $$("gisMtJournalDocumentStatus");

            if (statusButton) {
                if (String(document.crptDocumentId ?? "").length > 0)
                    statusButton.enable();
                else
                    statusButton.disable();
            }

            const unconfirmed = isUnconfirmed(document);
            const reconcileButton = $$("gisMtJournalDocumentReconcile");
            const notFoundButton = $$("gisMtJournalDocumentNotFound");

            if (reconcileButton) {
                if (unconfirmed)
                    reconcileButton.show();
                else
                    reconcileButton.hide();
            }

            if (notFoundButton) {
                if (unconfirmed)
                    notFoundButton.show();
                else
                    notFoundButton.hide();
            }
        } catch (error) {
            webix.message({ text: error?.message || "Документ не открыт", type: "error" });
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
                { view: "label", label: "Документ ГИС МТ" },
                { view: "icon", icon: "wxi-close", click: () => $$(WINDOW_ID).close() }
            ]
        },
        body: {
            padding: 10,
            rows: [
                {
                    view: "template",
                    id: "gisMtJournalDocumentBody",
                    borderless: true,
                    autoheight: true,
                    template: () => (currentDocument ? headerHtml(currentDocument) : "")
                },
                {
                    view: "datatable",
                    id: "gisMtJournalDocumentLines",
                    columns: [
                        { id: "cis", header: "КИ", fillspace: 3, template: obj => escape(obj.cis) }
                    ],
                    scroll: "y",
                    data: []
                },
                {
                    cols: [
                        {
                            view: "button",
                            id: "gisMtJournalDocumentEdit",
                            value: "Редактировать",
                            width: 160,
                            hidden: true,
                            click: () => {
                                if (!currentDocument)
                                    return;

                                $$(WINDOW_ID).close();
                                onEdit(currentDocument);
                            }
                        },
                        { view: "button", id: "gisMtJournalDocumentStatus", value: "Статус в ЧЗ", width: 160, disabled: true, click: () => checkStatus(id) },
                        { view: "button", id: "gisMtJournalDocumentReconcile", value: "Сверить с ЧЗ", width: 160, hidden: true, click: () => reconcileDocument(id) },
                        { view: "button", id: "gisMtJournalDocumentNotFound", value: "Нет в ЧЗ", width: 140, hidden: true, click: () => markNotFound(id) },
                        { view: "button", id: "gisMtJournalDocumentSend", value: "Отправить", width: 160, click: () => sendDocument(id) },
                        { view: "button", value: "Закрыть", width: 140, click: () => $$(WINDOW_ID).close() },
                        {}
                    ]
                }
            ]
        }
    }).show();

    load();
}
