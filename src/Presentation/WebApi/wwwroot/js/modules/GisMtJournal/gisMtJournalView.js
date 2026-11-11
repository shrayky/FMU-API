import { isMobileScanMode } from '../../utils/device.js';
import { loadProductGroupLabels, productGroupLabel } from '../../utils/productGroupLabels.js';

const JOURNAL_URL = "/api/ts/gismt/journal";
const DOCUMENTS_URL = "/api/ts/gismt/documents";

const KIND_LABELS = {
    CONNECT_TAP: "Постановка на кран",
    LK_RECEIPT: "Списание молочной продукции"
};

const REASON_LABELS = {
    PACKING: "Списание (фасовка)",
    EXPIRATION: "Списание (срок годности)"
};

const kindLabel = (item) => {
    if (item?.kind === "LK_RECEIPT")
        return REASON_LABELS[item.reason === "EXPIRATION" ? "EXPIRATION" : "PACKING"];

    return KIND_LABELS[item?.kind] ?? item?.kind ?? "";
};

const STATUS_LABELS = {
    draft: "Черновик",
    sending: "Отправляется",
    sent: "Отправлен",
    error: "Ошибка"
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

const crptStatusLabel = (code) => (code ? (CRPT_STATUS_LABELS[code] ?? code) : "");

const crptStatusColor = (code) => {
    switch (String(code ?? "").toUpperCase()) {
        case "CHECKED_OK":
            return "#2fbf71";
        case "CHECKED_NOT_OK":
        case "PARSE_ERROR":
        case "PROCESSING_ERROR":
            return "#dc3545";
        case "IN_PROGRESS":
        case "WAIT_ACCEPTANCE":
            return "#e0a030";
        case "ACCEPTED":
            return "#2fbf71";
        default:
            return "";
    }
};

const crptFailed = (code) => ["CHECKED_NOT_OK", "PARSE_ERROR", "PROCESSING_ERROR"].includes(String(code ?? "").toUpperCase());

const groupValue = (item) => {
    if (item?.productGroup)
        return item.productGroup;

    return item?.kind === "CONNECT_TAP" ? "beer" : "milk";
};

const TYPE_LABELS = {
    UNIVERSAL_TRANSFER_DOCUMENT: "УПД",
    UNIVERSAL_TRANSFER_DOCUMENT_FIX: "УПД (исправление)",
    UNIVERSAL_CORRECTION_DOCUMENT: "УКД",
    UNIVERSAL_CORRECTION_DOCUMENT_FIX: "УКД (исправление)"
};

class GisMtJournalView {
    constructor(id) {
        this.id = id;
        this.formName = "GisMtJournalView";
        this.outgoingPage = 1;
        this.incomingPage = 1;
        this.pageSize = 50;
        this.kind = "";
        this.status = "";

        this.NAMES = {
            table: "gisMtJournalTable",
            kindFilter: "gisMtJournalKindFilter",
            statusFilter: "gisMtJournalStatusFilter",
            pagination: "gisMtJournalPagination",
            prev: "gisMtJournalPrev",
            next: "gisMtJournalNext",
            sendButton: "gisMtJournalSend",
            statusButton: "gisMtJournalCrptStatus",
            openButton: "gisMtJournalOpen",
            deleteButton: "gisMtJournalDelete",
            receiptButton: "gisMtJournalReceipt",
            incomingTable: "gisMtIncomingTable",
            incomingPagination: "gisMtIncomingPagination",
            incomingPrev: "gisMtIncomingPrev",
            incomingNext: "gisMtIncomingNext",
            incomingRefresh: "gisMtIncomingRefresh"
        };

        this.LABELS = {
            title: "FMU-API: документы ГИС МТ",
            outgoingTab: "Внутренние документы (исходящие)",
            incomingTab: "УПД (входящие)",
            createReceipt: "Создать",
            open: "Открыть",
            delete: "Удалить",
            send: "Отправить",
            crptStatus: "Статус ЧЗ",
            refresh: "Обновить",
            kind: "Вид",
            productGroup: "Товарная группа",
            status: "Статус",
            all: "*",
            date: "Дата",
            inn: "ИНН",
            kpp: "КПП / ФИАС",
            marks: "Марок",
            crptId: "Документ ЧЗ",
            error: "Ошибка",
            created: "Создан",
            page: "Стр. ",
            incomingNumber: "Документ",
            incomingDocDate: "Дата документа",
            incomingType: "Тип",
            incomingSender: "Отправитель",
            incomingReceiver: "Получатель",
            incomingProductGroup: "Группа",
            incomingLoadedAt: "Загружен"
        };
    }

    loadConfig() {
        return this;
    }

    delayedDataLoading() {
        setTimeout(() => {
            this._loadOutgoing();
            this._loadIncoming();
        }, 300);

        return this;
    }

    render() {
        $$("toolbarLabel").setValue(this.LABELS.title);

        return {
            view: "form",
            id: this.id,
            name: this.formName,
            elements: [
                {
                    view: "tabview",
                    cells: [
                        { header: this.LABELS.outgoingTab, body: this._createOutgoingTab() },
                        { header: this.LABELS.incomingTab, body: this._createIncomingTab() }
                    ]
                }
            ]
        };
    }

    _createOutgoingTab() {
        return {
            rows: [
                this._createOutgoingToolbar(),
                this._createOutgoingTable(),
                {}
            ]
        };
    }

    _createIncomingTab() {
        return {
            rows: [
                this._createIncomingToolbar(),
                this._createIncomingTable(),
                {}
            ]
        };
    }

    _createOutgoingToolbar() {
        return {
            view: "toolbar",
            borderless: true,
            elements: [
                {
                    view: "button",
                    id: this.NAMES.receiptButton,
                    value: this.LABELS.createReceipt,
                    width: 140,
                    click: () => this._openCreateMenu()
                },
                {
                    view: "button",
                    id: this.NAMES.sendButton,
                    value: this.LABELS.send,
                    width: 140,
                    disabled: true,
                    click: () => this._sendSelected()
                },
                {
                    view: "button",
                    id: this.NAMES.openButton,
                    value: this.LABELS.open,
                    width: 120,
                    disabled: true,
                    click: () => this._openSelected()
                },
                {
                    view: "button",
                    id: this.NAMES.statusButton,
                    value: this.LABELS.crptStatus,
                    width: 140,
                    disabled: true,
                    click: () => this._checkCrptStatus()
                },
                {
                    view: "button",
                    id: this.NAMES.deleteButton,
                    value: this.LABELS.delete,
                    width: 120,
                    disabled: true,
                    click: () => this._deleteSelected()
                },
                {
                    view: "button",
                    value: this.LABELS.refresh,
                    width: 120,
                    click: () => this._loadOutgoing()
                },
                {
                    view: "richselect",
                    id: this.NAMES.kindFilter,
                    label: this.LABELS.kind,
                    labelWidth: 50,
                    width: 280,
                    value: this.LABELS.all,
                    options: [
                        { id: "*", value: "Все виды" },
                        { id: "CONNECT_TAP", value: KIND_LABELS.CONNECT_TAP },
                        { id: "LK_RECEIPT", value: KIND_LABELS.LK_RECEIPT }
                    ],
                    on: { onChange: () => this._onFilterChange() }
                },
                {
                    view: "richselect",
                    id: this.NAMES.statusFilter,
                    label: this.LABELS.status,
                    labelWidth: 60,
                    width: 220,
                    value: this.LABELS.all,
                    options: [
                        { id: "*", value: "Все статусы" },
                        { id: "draft", value: STATUS_LABELS.draft },
                        { id: "sending", value: STATUS_LABELS.sending },
                        { id: "sent", value: STATUS_LABELS.sent },
                        { id: "error", value: STATUS_LABELS.error }
                    ],
                    on: { onChange: () => this._onFilterChange() }
                },
                {},
                {
                    view: "button",
                    id: this.NAMES.prev,
                    value: "←",
                    width: 50,
                    disabled: true,
                    click: () => this._goToOutgoingPage(this.outgoingPage - 1)
                },
                { view: "label", id: this.NAMES.pagination, label: "", width: 140, align: "center" },
                {
                    view: "button",
                    id: this.NAMES.next,
                    value: "→",
                    width: 50,
                    disabled: true,
                    click: () => this._goToOutgoingPage(this.outgoingPage + 1)
                }
            ]
        };
    }

    _createIncomingToolbar() {
        return {
            view: "toolbar",
            borderless: true,
            elements: [
                {
                    view: "button",
                    id: this.NAMES.incomingRefresh,
                    value: this.LABELS.refresh,
                    width: 120,
                    click: () => this._loadIncoming()
                },
                {},
                {
                    view: "button",
                    id: this.NAMES.incomingPrev,
                    value: "←",
                    width: 50,
                    disabled: true,
                    click: () => this._goToIncomingPage(this.incomingPage - 1)
                },
                { view: "label", id: this.NAMES.incomingPagination, label: "", width: 140, align: "center" },
                {
                    view: "button",
                    id: this.NAMES.incomingNext,
                    value: "→",
                    width: 50,
                    disabled: true,
                    click: () => this._goToIncomingPage(this.incomingPage + 1)
                }
            ]
        };
    }

    _createOutgoingTable() {
        return {
            view: "datatable",
            id: this.NAMES.table,
            columns: [
                { id: "docDate", header: this.LABELS.date, width: 110, sort: "string" },
                {
                    id: "kind",
                    header: this.LABELS.kind,
                    fillspace: 1.6,
                    minWidth: 200,
                    template: obj => webix.template.escape(kindLabel(obj))
                },
                {
                    id: "productGroup",
                    header: this.LABELS.productGroup,
                    width: 180,
                    template: obj => webix.template.escape(productGroupLabel(groupValue(obj)))
                },
                {
                    id: "status",
                    header: this.LABELS.status,
                    width: 120,
                    template: obj => webix.template.escape(STATUS_LABELS[obj.status] ?? obj.status)
                },
                {
                    id: "crptStatus",
                    header: this.LABELS.crptStatus,
                    width: 170,
                    template: obj => {
                        const label = crptStatusLabel(obj.crptStatus);

                        if (!label)
                            return "";

                        const color = crptStatusColor(obj.crptStatus);

                        return color
                            ? `<span style="color:${color}">${webix.template.escape(label)}</span>`
                            : webix.template.escape(label);
                    }
                },
                { id: "inn", header: this.LABELS.inn, width: 130 },
                { id: "kppFias", header: this.LABELS.kpp, width: 140 },
                { id: "marks", header: this.LABELS.marks, width: 80 },
                { id: "crptDocumentId", header: this.LABELS.crptId, fillspace: 1.5, minWidth: 200 },
                {
                    id: "errorText",
                    header: this.LABELS.error,
                    fillspace: 2,
                    minWidth: 240,
                    template: obj => webix.template.escape(obj.errorText ?? "")
                },
                { id: "createdAt", header: this.LABELS.created, width: 160 }
            ],
            select: "row",
            autoheight: true,
            scroll: false,
            on: {
                onAfterSelect: () => this._refreshButtons(),
                onItemDblClick: (rowId) => this._openDocument(rowId)
            },
            data: []
        };
    }

    _createIncomingTable() {
        return {
            view: "datatable",
            id: this.NAMES.incomingTable,
            columns: [
                {
                    id: "number",
                    header: this.LABELS.incomingNumber,
                    width: 180,
                    template: obj => this._documentLink(obj.number)
                },
                { id: "docDate", header: this.LABELS.incomingDocDate, width: 130 },
                {
                    id: "type",
                    header: this.LABELS.incomingType,
                    width: 180,
                    template: obj => webix.template.escape(obj.type ?? "")
                },
                { id: "senderInn", header: this.LABELS.incomingSender, width: 130 },
                { id: "receiverInn", header: this.LABELS.incomingReceiver, width: 130 },
                {
                    id: "productGroup",
                    header: this.LABELS.incomingProductGroup,
                    fillspace: 2,
                    minWidth: 180,
                    template: obj => webix.template.escape(obj.productGroup ?? "")
                },
                { id: "marksCount", header: this.LABELS.marks, width: 80 },
                { id: "loadedAt", header: this.LABELS.incomingLoadedAt, width: 170 }
            ],
            select: false,
            autoheight: true,
            scroll: false,
            on: {
                onItemClick: (rowId, event) => this._onIncomingDocumentClick(rowId, event),
                onItemDblClick: (rowId) => this._openIncomingDocument(this._incomingRowNumber(rowId))
            },
            data: []
        };
    }

    _documentLink(value) {
        const number = String(value ?? "");

        if (!number)
            return "";

        return `<span class="gis-mt-document-link" style="color:#3d8fe0;cursor:pointer;text-decoration:underline">${webix.template.escape(number)}</span>`;
    }

    _onIncomingDocumentClick(rowId, event) {
        const className = String(event?.target?.className ?? "");

        if (!className.includes("gis-mt-document-link"))
            return;

        if (event?.preventDefault)
            event.preventDefault();

        this._openIncomingDocument(this._incomingRowNumber(rowId));
    }

    _incomingRowNumber(rowId) {
        const table = $$(this.NAMES.incomingTable);
        const row = table ? table.getItem(rowId) : null;

        return row?.number;
    }

    _openIncomingDocument(number) {
        if (!number)
            return;

        import("../GisMtMarks/gisMtDocumentWindow.js").then(module => module.default(number));
    }

    _selectedKindValue() {
        const value = $$(this.NAMES.kindFilter).getValue();
        return !value || value === "*" ? "" : value;
    }

    _selectedStatusValue() {
        const value = $$(this.NAMES.statusFilter).getValue();
        return !value || value === "*" ? "" : value;
    }

    _onFilterChange() {
        this.kind = this._selectedKindValue();
        this.status = this._selectedStatusValue();
        this.outgoingPage = 1;
        this._loadOutgoing();
    }

    _refreshButtons() {
        const table = $$(this.NAMES.table);
        const selected = table ? table.getSelectedItem() : null;
        const sendButton = $$(this.NAMES.sendButton);
        const openButton = $$(this.NAMES.openButton);
        const deleteButton = $$(this.NAMES.deleteButton);
        const statusButton = $$(this.NAMES.statusButton);
        const locked = !!selected && (selected.status === "sent" || selected.status === "sending");
        const canSend = !!selected && (selected.status === "draft" || selected.status === "error");
        const canDelete = !!selected
            && selected.status !== "sending"
            && (selected.status !== "sent" || crptFailed(selected.crptStatus));

        if (statusButton) {
            if (selected && String(selected.crptDocumentId || "").length > 0)
                statusButton.enable();
            else
                statusButton.disable();
        }

        if (openButton) {
            if (selected)
                openButton.enable();
            else
                openButton.disable();
        }

        if (deleteButton) {
            if (canDelete)
                deleteButton.enable();
            else
                deleteButton.disable();
        }

        if (!sendButton)
            return;

        if (canSend)
            sendButton.enable();
        else
            sendButton.disable();
    }

    async _deleteSelected() {
        const table = $$(this.NAMES.table);
        const selected = table ? table.getSelectedItem() : null;

        if (!selected)
            return;

        const confirmed = await webix.confirm({
            title: "Удаление документа",
            text: crptFailed(selected.crptStatus)
                ? "Документ отклонён Честным знаком. Удалить его из журнала?"
                : "Удалить акт из журнала? Отменить это действие нельзя.",
            ok: "Удалить",
            cancel: "Отмена"
        }).catch(() => false);

        if (!confirmed)
            return;

        try {
            const response = await fetch(`${JOURNAL_URL}/${encodeURIComponent(selected.id)}`, { method: "DELETE" });
            const body = await response.json().catch(() => ({}));

            if (!response.ok) {
                webix.message({ text: body?.error || "Документ не удалён", type: "error", expire: 8000 });
                return;
            }

            webix.message("Документ удалён");
            this._loadOutgoing();
        } catch (error) {
            webix.message({ text: error?.message || "Документ не удалён", type: "error" });
        }
    }

    _openSelected() {
        const table = $$(this.NAMES.table);
        const selected = table ? table.getSelectedItem() : null;

        if (selected)
            this._openDocument(selected.id);
    }

    _openDocument(rowId) {
        if (!rowId)
            return;

        import("./gisMtJournalDocumentWindow.js").then(module => {
            module.default({
                id: rowId,
                onChanged: () => this._loadOutgoing(),
                onEdit: (document) => this._editDraft(document)
            });
        });
    }

    _editDraft(document) {
        import("./gisMtJournalReceiptView.js").then(module => {
            module.default({
                documentId: document.id,
                reason: document.reason,
                onSaved: () => this._loadOutgoing()
            });
        });
    }

    _goToOutgoingPage(page) {
        if (page < 1)
            return;

        this.outgoingPage = page;
        this._loadOutgoing();
    }

    _goToIncomingPage(page) {
        if (page < 1)
            return;

        this.incomingPage = page;
        this._loadIncoming();
    }

    async _loadOutgoing() {
        const table = $$(this.NAMES.table);

        if (!table)
            return;

        this._showProgress(this.NAMES.table);

        const url = new URL(JOURNAL_URL, window.location.origin);
        url.searchParams.set("page", this.outgoingPage.toString());
        url.searchParams.set("pageSize", this.pageSize.toString());

        if (this.kind)
            url.searchParams.set("kind", this.kind);

        if (this.status)
            url.searchParams.set("status", this.status);

        let data;

        try {
            const response = await fetch(url);
            const body = await response.json();

            if (!response.ok) {
                this._hideProgress(this.NAMES.table);

                if (!isMobileScanMode())
                    webix.message({ text: body?.error || "Ошибка загрузки журнала", type: "error" });

                return;
            }

            data = body;
        } catch (error) {
            this._hideProgress(this.NAMES.table);
            return;
        }

        await loadProductGroupLabels();
        this._updateOutgoingTable(data);
        this._updatePagination(data, {
            info: this.NAMES.pagination,
            prev: this.NAMES.prev,
            next: this.NAMES.next
        });
        this._hideProgress(this.NAMES.table);    }

    async _loadIncoming() {
        const table = $$(this.NAMES.incomingTable);

        if (!table)
            return;

        this._showProgress(this.NAMES.incomingTable);

        const url = new URL(DOCUMENTS_URL, window.location.origin);
        url.searchParams.set("page", this.incomingPage.toString());
        url.searchParams.set("pageSize", this.pageSize.toString());

        let data;

        try {
            const response = await fetch(url);
            const body = await response.json();

            if (!response.ok) {
                this._hideProgress(this.NAMES.incomingTable);

                if (!isMobileScanMode())
                    webix.message({ text: body?.error || "Ошибка загрузки входящих документов", type: "error" });

                return;
            }

            data = body;
        } catch (error) {
            this._hideProgress(this.NAMES.incomingTable);
            return;
        }

        await loadProductGroupLabels();
        this._updateIncomingTable(data);
        this._updatePagination(data, {
            info: this.NAMES.incomingPagination,
            prev: this.NAMES.incomingPrev,
            next: this.NAMES.incomingNext
        });
        this._hideProgress(this.NAMES.incomingTable);
    }

    _updateOutgoingTable(data) {
        const table = $$(this.NAMES.table);

        if (!table)
            return;

        const rows = (data.documents || []).map(document => ({
            id: document.id,
            docDate: document.docDate ?? "",
            kind: document.kind,
            productGroup: document.productGroup || "",
            reason: document.reason ?? "",
            status: document.status,
            inn: document.inn,
            kppFias: document.kpp || document.fiasId || "",
            marks: (document.lines || []).length,
            crptDocumentId: document.crptDocumentId || "",
            crptStatus: document.crptStatus || "",
            errorText: document.errorText || "",
            createdAt: document.createdAt ? new Date(document.createdAt).toLocaleString() : ""
        }));

        table.clearAll();
        table.parse(rows);
        table.sort("docDate", "desc", "string");
        table.resize();
        this._refreshButtons();
    }

    _updateIncomingTable(data) {
        const table = $$(this.NAMES.incomingTable);

        if (!table)
            return;

        const rows = (data.documents || []).map(document => ({
            id: document.id,
            number: document.number,
            docDate: document.docDate ? new Date(document.docDate).toLocaleDateString() : "",
            type: TYPE_LABELS[document.type] ?? document.type ?? "",
            senderInn: document.senderInn ?? "",
            receiverInn: document.receiverInn ?? "",
            productGroup: productGroupLabel(document.productGroup),
            marksCount: document.marksCount ?? 0,
            loadedAt: document.loadedAt ? new Date(document.loadedAt).toLocaleString() : ""
        }));

        table.clearAll();
        table.parse(rows);
        table.resize();
    }

    _updatePagination(data, ids) {
        const info = $$(ids.info);
        const prev = $$(ids.prev);
        const next = $$(ids.next);
        const currentPage = data.currentPage || 1;
        const totalPages = data.totalPages || 1;

        if (info)
            info.setValue(`${this.LABELS.page}${currentPage} / ${totalPages} (${data.count || 0})`);

        if (prev) {
            if (currentPage <= 1)
                prev.disable();
            else
                prev.enable();
        }

        if (next) {
            if (currentPage >= totalPages)
                next.disable();
            else
                next.enable();
        }
    }

    _showProgress(id) {
        const view = $$(id);

        if (!view)
            return;

        webix.extend(view, webix.ProgressBar);
        view.showProgress({ type: "icon" });
    }

    _hideProgress(id) {
        const view = $$(id);

        if (view && view.hideProgress)
            view.hideProgress();
    }

    _openCreateMenu() {
        const windowId = "gisMtJournalCreateWindow";
        const previous = $$(windowId);

        if (previous)
            previous.destructor();

        const close = () => $$(windowId).close();

        const choose = (reason) => {
            close();
            this._openReceipt(reason);
        };

        webix.ui({
            view: "window",
            id: windowId,
            position: "center",
            modal: true,
            move: false,
            width: 420,
            height: 250,
            head: {
                view: "toolbar",
                elements: [
                    { view: "label", label: "Что создать" },
                    { view: "icon", icon: "wxi-close", click: () => close() }
                ]
            },
            body: {
                padding: 16,
                rows: [
                    {
                        view: "button",
                        value: REASON_LABELS.PACKING,
                        height: 44,
                        click: () => choose("PACKING")
                    },
                    {
                        view: "button",
                        value: REASON_LABELS.EXPIRATION,
                        height: 44,
                        click: () => choose("EXPIRATION")
                    },
                    {
                        cols: [
                            { view: "button", value: "Отмена", width: 140, click: () => close() },
                            {}
                        ]
                    }
                ]
            }
        }).show();
    }

    _openReceipt(reason = "PACKING") {
        import("./gisMtJournalReceiptView.js").then(module => {
            module.default({ reason, onSaved: () => this._loadOutgoing() });
        });
    }

    async _checkCrptStatus() {
        const table = $$(this.NAMES.table);
        const selected = table ? table.getSelectedItem() : null;

        if (!selected)
            return;

        try {
            const response = await fetch(`${JOURNAL_URL}/${encodeURIComponent(selected.id)}/status`, { method: "POST" });
            const body = await response.json();

            if (!response.ok) {
                webix.message({ text: body?.error || "Статус в Честном знаке не получен", type: "error", expire: 8000 });
                return;
            }

            const errors = String(body?.errors ?? "").trim();
            const status = body?.statusText || crptStatusLabel(body?.status);
            const text = errors.length > 0
                ? `Статус в ЧЗ: ${status}. ${errors}`
                : `Статус в ЧЗ: ${status}`;

            webix.message({ text, type: errors.length > 0 ? "error" : "info", expire: errors.length > 0 ? 12000 : 6000 });
            this._loadOutgoing();
        } catch (error) {
            webix.message({ text: error?.message || "Статус в Честном знаке не получен", type: "error" });
        }
    }

    async _sendSelected() {
        const table = $$(this.NAMES.table);
        const selected = table ? table.getSelectedItem() : null;

        if (!selected)
            return;

        if (selected.status === "sent" || selected.status === "sending") {
            webix.message({
                text: selected.status === "sending" ? "Документ уже отправляется" : "Документ уже отправлен",
                type: "warning",
                expire: 5000
            });
            return;
        }

        const hint = selected.kind === "LK_RECEIPT" && selected.reason === "EXPIRATION"
            ? " Марки после отправки будут помечены проданными."
            : "";

        const confirmed = await webix.confirm({
            title: "Отправка документа",
            text: `Отправить в Честный знак: ${kindLabel(selected)}?${hint}`,
            ok: "Отправить",
            cancel: "Отмена"
        }).catch(() => false);

        if (!confirmed)
            return;

        const sendButton = $$(this.NAMES.sendButton);

        if (sendButton)
            sendButton.disable();

        this._showProgress(this.NAMES.table);

        try {
            const response = await fetch(`${JOURNAL_URL}/${encodeURIComponent(selected.id)}/send`, { method: "POST" });
            const body = await response.json();

            if (!response.ok) {
                webix.message({ text: body?.error || "Документ не отправлен", type: "error", expire: 8000 });
                return;
            }

            webix.message("Документ отправлен");
        } catch (error) {
            webix.message({ text: error?.message || "Документ не отправлен", type: "error" });
        } finally {
            this._hideProgress(this.NAMES.table);
            this._loadOutgoing();
        }
    }
}

export default function gisMtJournalView(id) {
    return new GisMtJournalView(id)
        .loadConfig()
        .delayedDataLoading()
        .render();
}
