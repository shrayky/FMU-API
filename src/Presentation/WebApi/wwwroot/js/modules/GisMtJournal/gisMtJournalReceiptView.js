import { loadParameters } from '../../services/ConfigurationService.js';
import { ScannerWedge } from '../../utils/scannerWedge.js';
import { loadProductGroupLabels, productGroupLabel } from '../../utils/productGroupLabels.js';

const JOURNAL_URL = "/api/ts/gismt/journal";
const WINDOW_ID = "gisMtJournalReceiptWindow";
const INN_ID = "gisMtJournalReceiptInn";
const MARK_INPUT_ID = "gisMtJournalReceiptMark";
const GROUP_ID = "gisMtJournalReceiptGroup";
const EXPIRED_LABEL_ID = "gisMtJournalReceiptExpired";
const FILL_BUTTON_ID = "gisMtJournalReceiptFill";
const LINES_TABLE_ID = "gisMtJournalReceiptLines";
const ADD_BUTTON_ID = "gisMtJournalReceiptAdd";
const SAVE_BUTTON_ID = "gisMtJournalReceiptSave";
const SEND_BUTTON_ID = "gisMtJournalReceiptSend";

const BUSY_CONTROLS = [INN_ID, GROUP_ID, MARK_INPUT_ID, ADD_BUTTON_ID, FILL_BUTTON_ID, SAVE_BUTTON_ID, SEND_BUTTON_ID, LINES_TABLE_ID];

const REASON_PACKING = "PACKING";
const REASON_EXPIRATION = "EXPIRATION";
const EXPIRED_LIMIT = 500;

const TITLES = {
    [REASON_PACKING]: "Списание весовой молочной продукции",
    [REASON_EXPIRATION]: "Списание по истечении срока годности"
};

const formatDate = (value) => {
    if (!value)
        return "";

    const date = new Date(value);

    return Number.isNaN(date.getTime()) ? String(value) : date.toLocaleDateString();
};

export default function openGisMtJournalReceipt(options = {}) {
    const documentId = options.documentId ?? null;
    const reason = options.reason === REASON_EXPIRATION ? REASON_EXPIRATION : REASON_PACKING;
    const expiration = reason === REASON_EXPIRATION;
    const onSaved = options.onSaved ?? (() => {});

    const existing = $$(WINDOW_ID);

    if (existing)
        existing.destructor();

    const lines = [];
    let initializing = true;
    let expiredTotal = 0;

    const refreshLines = () => {
        const table = $$(LINES_TABLE_ID);

        if (!table)
            return;

        table.clearAll();
        table.parse(lines.map((line, index) => ({
            id: index + 1,
            mark: line.mark,
            productName: line.productName,
            cis: line.cis,
            gtin: line.gtin,
            quantity: line.quantityKg == null ? "" : Number(line.quantityKg).toFixed(3),
            price: line.pricePerKg == null ? "" : Number(line.pricePerKg).toFixed(2),
            amount: line.amountKop == null ? "" : (line.amountKop / 100).toFixed(2),
            expireDate: formatDate(line.expireDate),
            error: line.error
        })));

        const hasErrors = lines.some(line => String(line.error || "").trim().length > 0);

        if (hasErrors)
            table.showColumn("error");
        else
            table.hideColumn("error");

        table.resize();
        table.clearSelection();
        syncControls();
    };

    const syncControls = () => {
        const organisation = $$(INN_ID);
        const chosen = !!(organisation && organisation.getValue());
        const addButton = $$(ADD_BUTTON_ID);
        const markInput = $$(MARK_INPUT_ID);
        const group = $$(GROUP_ID);
        const fillButton = $$(FILL_BUTTON_ID);

        if (addButton) {
            if (chosen)
                addButton.enable();
            else
                addButton.disable();
        }

        if (markInput) {
            if (chosen)
                markInput.enable();
            else
                markInput.disable();
        }

        if (fillButton) {
            const groupChosen = !!(group && group.getValue());

            if (chosen && groupChosen && lines.length < EXPIRED_LIMIT)
                fillButton.enable();
            else
                fillButton.disable();
        }
    };

    const setBusy = (busy) => {
        const window$ = $$(WINDOW_ID);

        if (window$) {
            if (busy) {
                webix.extend(window$, webix.ProgressBar);
                window$.showProgress({ type: "icon" });
            } else if (window$.hideProgress) {
                window$.hideProgress();
            }
        }

        BUSY_CONTROLS.forEach(id => {
            const control = $$(id);

            if (!control)
                return;

            if (busy)
                control.disable();
            else
                control.enable();
        });

        if (!busy)
            syncControls();
    };

    const reportScanWarnings = (meta = {}) => {
        const warnings = [];

        if (meta.capsLock)
            warnings.push("Включён Caps Lock — раскладка сканера может исказить код");

        if (meta.cyrillic)
            warnings.push("В штрихкоде есть русские символы — проверьте раскладку клавиатуры");

        if (warnings.length > 0)
            webix.message({ text: warnings.join(". "), type: "error", expire: 5000 });
    };

    const preview = async () => {
        const organisation = $$(INN_ID);

        if (expiration || !organisation || !organisation.getValue() || lines.length === 0)
            return;

        try {
            const response = await fetch(`${JOURNAL_URL}/receipt/preview`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ inn: organisation.getValue(), marks: lines.map(line => line.mark) })
            });

            const body = await response.json();

            if (!response.ok) {
                webix.message({ text: body?.error || "Не удалось получить КИ, количество и сумму марок", type: "error", expire: 8000 });
                return;
            }

            (body || []).forEach((item, index) => {
                const line = lines[index];

                if (!line)
                    return;

                line.cis = item.cis ?? "";
                line.gtin = item.gtin ?? "";
                line.productName = item.productName ?? "";
                line.quantityKg = item.quantityKg ?? null;
                line.pricePerKg = item.pricePerKg ?? null;
                line.amountKop = item.amountKop ?? null;
                line.error = item.error ?? "";
            });

            refreshLines();
        } catch (error) {
            webix.message({ text: error?.message || "Не удалось получить КИ, количество и сумму марок", type: "error" });
        }
    };

    const addMarkValue = async (mark, meta = {}) => {
        const organisation = $$(INN_ID);

        if (!organisation || !organisation.getValue()) {
            webix.message({ text: "Сначала выберите организацию", type: "warning", expire: 4000 });
            return;
        }

        const value = String(mark || "").trim();

        if (!value)
            return;

        if (lines.some(line => line.mark === value)) {
            webix.message({ text: "Эта марка уже добавлена", type: "info", expire: 3000 });
            return;
        }

        lines.push({
            mark: value,
            cis: "",
            gtin: "",
            productName: "",
            quantityKg: null,
            pricePerKg: null,
            amountKop: null,
            expireDate: null,
            error: ""
        });

        reportScanWarnings(meta);
        refreshLines();
        await preview();
    };

    const addMarkFromInput = () => {
        const input = $$(MARK_INPUT_ID);

        if (!input)
            return;

        const mark = (input.getValue() || "").trim();

        if (!mark)
            return;

        input.setValue("");
        addMarkValue(mark);
    };

    const fillExpired = async () => {
        const organisation = $$(INN_ID);
        const group = $$(GROUP_ID);

        if (!organisation || !organisation.getValue()) {
            webix.message({ text: "Сначала выберите организацию", type: "warning", expire: 4000 });
            return;
        }

        if (!group || !group.getValue()) {
            webix.message({ text: "Выберите товарную группу", type: "warning", expire: 4000 });
            return;
        }

        const button = $$(FILL_BUTTON_ID);

        if (button)
            button.disable();

        setBusy(true);

        try {
            const response = await fetch(`${JOURNAL_URL}/receipt/expired`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ inn: organisation.getValue(), productGroup: group.getValue() })
            });

            const body = await response.json();

            if (!response.ok) {
                webix.message({ text: body?.error || "Просроченные марки не подобраны", type: "error", expire: 8000 });
                return;
            }

            const marks = Array.isArray(body) ? body : (body?.marks || []);
            const excludedPackages = Number(body?.excludedPackages ?? 0);

            let added = 0;

            (marks || []).forEach(item => {
                if (lines.length >= EXPIRED_LIMIT)
                    return;

                const mark = String(item.mark ?? "").trim();
                const cis = String(item.cis ?? "").trim();

                if (!mark || !cis || lines.some(line => line.cis === cis))
                    return;

                lines.push({
                    mark,
                    cis,
                    gtin: item.gtin ?? "",
                    productName: "",
                    quantityKg: null,
                    pricePerKg: null,
                    amountKop: null,
                    expireDate: item.expireDate ?? null,
                    error: ""
                });

                added++;
            });

            refreshLines();

            const parts = [];

            if (added > 0)
                parts.push(`Подобрано марок: ${added}`);

            if (excludedPackages > 0)
                parts.push(`исключено упаковок: ${excludedPackages}`);

            if (parts.length === 0)
                parts.push("Новых просроченных марок не найдено");

            setFillLabel(added > 0 ? `В документе: ${lines.length}` : "Просроченных нет");
            webix.message(parts.join(", "));
        } catch (error) {
            webix.message({ text: error?.message || "Просроченные марки не подобраны", type: "error" });
        } finally {
            setBusy(false);
            syncControls();
        }
    };

    const deleteLine = (rowId) => {
        const index = Number(rowId) - 1;

        if (index < 0 || index >= lines.length)
            return;

        lines.splice(index, 1);
        refreshLines();
    };

    const scanner = new ScannerWedge({
        timeoutMs: 50,
        onScan: (code, meta) => {
            if (expiration)
                return;

            addMarkValue(code, meta);
        }
    });

    const save = async (send) => {
        const organisation = $$(INN_ID).getValue();

        if (!organisation) {
            webix.message({ text: "Выберите организацию", type: "error" });
            return;
        }

        if (lines.length === 0) {
            webix.message({ text: "В документе нет ни одной марки", type: "error" });
            return;
        }

        const productGroup = expiration ? $$(GROUP_ID).getValue() : "";

        if (expiration && !productGroup) {
            webix.message({ text: "Выберите товарную группу", type: "error" });
            return;
        }

        const url = expiration
            ? (documentId
                ? `${JOURNAL_URL}/${encodeURIComponent(documentId)}/receipt/expiration`
                : `${JOURNAL_URL}/receipt/expiration`)
            : (documentId
                ? `${JOURNAL_URL}/${encodeURIComponent(documentId)}/receipt`
                : `${JOURNAL_URL}/receipt`);

        const payload = expiration
            ? { inn: organisation, productGroup, lines: lines.map(line => ({ mark: line.mark, cis: line.cis })) }
            : { inn: organisation, marks: lines.map(line => line.mark) };

        setBusy(true);

        try {
            const response = await fetch(url, {
                method: documentId ? "PUT" : "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload)
            });

            const body = await response.json();

            if (!response.ok) {
                webix.message({ text: body?.error || "Документ не сохранён", type: "error", expire: 8000 });
                return;
            }

            if ((body.lineErrors || []).length > 0)
                webix.message({ text: body.lineErrors.join("; "), type: "warning", expire: 10000 });

            const savedId = body.document.id;

            if (send) {
                const sent = await fetch(`${JOURNAL_URL}/${encodeURIComponent(savedId)}/send`, { method: "POST" });
                const sentBody = await sent.json();

                if (!sent.ok) {
                    webix.message({ text: sentBody?.error || "Документ не отправлен", type: "error", expire: 8000 });
                } else {
                    webix.message("Документ отправлен");
                }
            } else {
                webix.message(documentId ? "Черновик сохранён" : "Документ сохранён черновиком");
            }

            $$(WINDOW_ID).close();
            onSaved();
        } catch (error) {
            webix.message({ text: error?.message || "Документ не сохранён", type: "error" });
        } finally {
            setBusy(false);
        }
    };

    webix.ui({
        view: "window",
        id: WINDOW_ID,
        position: "center",
        modal: true,
        move: false,
        width: Math.min(window.innerWidth * 0.9, 1100),
        height: 560,
        on: {
            onDestruct: () => scanner.stop(),
            onHide: () => scanner.stop()
        },
        head: {
            view: "toolbar",
            elements: [
                { view: "label", label: documentId ? `Черновик: ${TITLES[reason]}` : TITLES[reason] },
                { view: "icon", icon: "wxi-close", click: () => $$(WINDOW_ID).close() }
            ]
        },
        body: {
            padding: 10,
            rows: [
                {
                    view: "richselect",
                    id: INN_ID,
                    label: "Организация",
                    labelWidth: 120,
                    options: [],
                    on: {
                        onChange: () => {
                            syncControls();
                            loadGroups();
                        }
                    }
                },
                {
                    hidden: !expiration,
                    cols: [
                        {
                            view: "richselect",
                            id: GROUP_ID,
                            label: "Товарная группа",
                            labelWidth: 120,
                            options: [],
                            on: {
                                onChange: () => onGroupChange()
                            }
                        },
                        { view: "button", id: FILL_BUTTON_ID, value: "Заполнить", width: 160, click: () => fillExpired() },
                        { view: "label", id: EXPIRED_LABEL_ID, label: "", width: 260 },
                        {}
                    ]
                },
                {
                    hidden: expiration,
                    cols: [
                        {
                            view: "text",
                            id: MARK_INPUT_ID,
                            placeholder: "Сканируйте марку или введите код и нажмите Enter",
                            on: {
                                onEnter: () => addMarkFromInput()
                            }
                        },
                        { view: "button", id: ADD_BUTTON_ID, value: "Добавить", width: 140, click: () => addMarkFromInput() },
                        {}
                    ]
                },
                {
                    view: "datatable",
                    id: LINES_TABLE_ID,
                    columns: [
                        {
                            id: "delete",
                            header: "",
                            width: 44,
                            template: () => "<span class=\"webix_icon wxi-trash\" style=\"cursor: pointer; color: #dc3545;\" title=\"Удалить марку\"></span>"
                        },
                        { id: "mark", header: "Марка", fillspace: 2, template: obj => webix.template.escape(obj.mark) },
                        {
                            id: "productName",
                            header: "Название",
                            fillspace: 2,
                            hidden: expiration,
                            template: obj => webix.template.escape(obj.productName)
                        },
                        { id: "cis", header: "КИ", fillspace: 2, template: obj => webix.template.escape(obj.cis) },
                        { id: "gtin", header: "GTIN", width: 150, template: obj => webix.template.escape(obj.gtin) },
                        { id: "quantity", header: "Кол-во, кг", width: 100, hidden: expiration },
                        { id: "price", header: "Цена, ₽/кг", width: 110, hidden: expiration },
                        { id: "amount", header: "Сумма, ₽", width: 110, hidden: expiration },
                        { id: "expireDate", header: "Срок годности", width: 140, hidden: !expiration },
                        {
                            id: "error",
                            header: "Ошибка",
                            fillspace: 1.5,
                            hidden: true,
                            template: obj => webix.template.escape(obj.error)
                        }
                    ],
                    select: true,
                    scroll: "y",
                    on: {
                        onItemClick: (rowId, event) => {
                            const className = String((event?.target?.className) || "");

                            if (!className.includes("wxi-trash"))
                                return;

                            if (event?.preventDefault)
                                event.preventDefault();

                            deleteLine(rowId);
                        }
                    },
                    data: []
                },
                {
                    cols: [
                        { view: "button", id: SAVE_BUTTON_ID, value: "Сохранить", width: 160, click: () => save(false) },
                        { view: "button", id: SEND_BUTTON_ID, value: "Сохранить и отправить", width: 220, click: () => save(true) },
                        { view: "button", value: "Закрыть", width: 140, click: () => $$(WINDOW_ID).close() },
                        {}
                    ]
                }
            ]
        }
    }).show();

    if (!expiration)
        scanner.start();

    syncControls();

    const loadOrganisations = async () => {
        try {
            const config = await loadParameters();
            const organisations = config?.organisationConfig?.printGroups ?? [];
            const select = $$(INN_ID);

            if (!select)
                return;

            select.define("options", organisations.map(item => ({ id: item.inn, value: `${item.name} (${item.inn})` })));
            select.refresh();

            if (organisations.length === 1) {
                select.setValue(organisations[0].inn);
                select.disable();
            }

            syncControls();
        } catch {
            webix.message({ text: "Не удалось загрузить организации", type: "error" });
        }
    };

    const loadGroups = async () => {
        const select = $$(GROUP_ID);
        const organisation = $$(INN_ID);

        if (!select || !organisation || !organisation.getValue())
            return;

        select.define("options", []);
        select.setValue("");
        select.refresh();
        setFillLabel("");

        if (!expiration)
            return;

        try {
            const url = new URL(`${JOURNAL_URL}/receipt/groups`, window.location.origin);
            url.searchParams.set("inn", organisation.getValue());

            const response = await fetch(url);
            const body = await response.json();

            if (!response.ok) {
                webix.message({ text: body?.error || "Товарные группы не получены", type: "error", expire: 8000 });
                return;
            }

            await loadProductGroupLabels();

            select.define("options", (body || [])
                .filter(group => String(group ?? "").length > 0)
                .map(group => ({
                    id: group,
                    value: productGroupLabel(group)
                })));
            select.refresh();

            if ((body || []).length === 1) {
                select.setValue(body[0]);
                select.refresh();
            }

            if ((body || []).length === 0)
                webix.message({ text: "Остатков марок в ГИС МТ нет", type: "info", expire: 5000 });
        } catch (error) {
            webix.message({ text: error?.message || "Товарные группы не получены", type: "error" });
        } finally {
            syncControls();
        }
    };

    const setFillLabel = (text) => {
        const label = $$(EXPIRED_LABEL_ID);

        if (label)
            label.setValue(text);
    };

    const onGroupChange = () => {
        if (initializing) {
            syncControls();
            return;
        }

        loadExpiredCount();
    };

    const loadExpiredCount = async () => {
        const organisation = $$(INN_ID);
        const group = $$(GROUP_ID);

        expiredTotal = 0;
        setFillLabel("");

        if (!expiration || !organisation || !organisation.getValue() || !group || !group.getValue()) {
            syncControls();
            return;
        }

        try {
            const url = new URL(`${JOURNAL_URL}/receipt/expired-count`, window.location.origin);
            url.searchParams.set("inn", organisation.getValue());
            url.searchParams.set("productGroup", group.getValue());

            const response = await fetch(url);
            const body = await response.json();

            if (!response.ok) {
                webix.message({ text: body?.error || "Просроченные марки не посчитаны", type: "error", expire: 8000 });
                return;
            }

            expiredTotal = Number(body?.count ?? 0);
            setFillLabel(expiredTotal > 0 ? `Просрочено: ${expiredTotal}` : "Просроченных нет");
        } catch (error) {
            webix.message({ text: error?.message || "Просроченные марки не посчитаны", type: "error" });
        } finally {
            syncControls();
        }
    };

    const loadDocument = async () => {
        if (!documentId)
            return;

        try {
            const response = await fetch(`${JOURNAL_URL}/${encodeURIComponent(documentId)}`);
            const document = await response.json();

            if (!response.ok) {
                webix.message({ text: document?.error || "Документ не открыт", type: "error", expire: 8000 });
                return;
            }

            (document.lines || []).forEach(line => {
                const cis = line.cis ?? "";

                if (cis && lines.some(existing => existing.cis === cis))
                    return;

                lines.push({
                    mark: line.mark || cis,
                    cis,
                    gtin: line.gtin ?? "",
                    productName: line.productName ?? "",
                    quantityKg: line.quantityKg ?? null,
                    pricePerKg: line.pricePerKg ?? null,
                    amountKop: line.amountKop ?? null,
                    expireDate: null,
                    error: ""
                });
            });

            const select = $$(INN_ID);

            if (select && document.inn) {
                select.setValue(document.inn);
                select.refresh();
            }

            const group = $$(GROUP_ID);

            if (expiration && group && document.productGroup) {
                group.setValue(document.productGroup);
                group.refresh();
            }

            refreshLines();
            await preview();
        } catch (error) {
            webix.message({ text: error?.message || "Документ не открыт", type: "error" });
        }
    };

    loadOrganisations()
        .then(loadGroups)
        .then(loadDocument)
        .finally(() => {
            initializing = false;
            syncControls();
            loadExpiredCount();
        });
}
