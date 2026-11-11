import { ScannerWedge } from '../../utils/scannerWedge.js';
import { loadParameters } from '../../services/ConfigurationService.js';
import { loadProductGroupLabels, productGroupLabel } from '../../utils/productGroupLabels.js';

class GisMtMarksView {
    constructor(id) {
        this.formName = "GisMtMarksView";
        this.id = id;

        this.marksApiAddress = "/api/ts/gismt/marks";
        this.ALL_GROUPS = "*";
        this.ALL_INNS = "*";
        this.STOCK_SOURCE_ID = "stock";
        this.CIRCULATION_ANY = "*";
        this.CIRCULATION_IN = "in";
        this.CIRCULATION_OUT = "out";
        this.currentPage = 1;
        this.pageSize = 50;
        this.searchTerm = "";
        this.productGroup = "";
        this.organisationInn = "";
        this.expiredOnly = false;
        this.circulation = this.CIRCULATION_ANY;
        this.filtersLoading = false;
        this.productGroupOptions = [{ id: "*", value: "Все группы" }];
        this.organisationOptions = [{ id: "*", value: "Все ИНН" }];

        this.LABELS = {
            formTitle: "FMU-API: Остатки марок ГИС МТ",
            search: "Поиск по sGTIN / КИ",
            productGroupFilter: "Группа",
            organisationFilter: "ИНН",
            organisationAll: "Все ИНН",
            expiredFilter: "Просрочен",
            expiredTooltip: "Только марки с истёкшим сроком годности",
            circulationFilter: "В обороте",
            circulationAny: "Все",
            circulationIn: "Да",
            circulationOut: "Нет",
            cis: "КИ",
            sgtin: "sGTIN",
            status: "Статус",
            circulation: "В обороте",
            expireDate: "Срок годности",
            ownerInn: "ИНН владельца",
            productGroup: "Группа",
            infoLoadedAt: "Дата загрузки сведений",
            sourceDocumentId: "Документ",
            page: "Стр. "
        };

        this.NAMES = {
            searchInput: "gisMtSearchInput",
            productGroupFilter: "gisMtProductGroupFilter",
            organisationFilter: "gisMtOrganisationFilter",
            expiredFilter: "gisMtExpiredFilter",
            circulationFilter: "gisMtCirculationFilter",
            marksTable: "gisMtMarksTable",
            paginationInfo: "gisMtPaginationInfo",
            prevButton: "gisMtPrevButton",
            nextButton: "gisMtNextButton"
        };

        this.scanner = new ScannerWedge({
            timeoutMs: 50,
            onScan: (code, meta) => this._onScan(code, meta)
        });
    }

    loadConfig() {
        return this;
    }

    delayedDataLoading() {
        setTimeout(async () => {
            await this._loadFilterOptions();
            this._loadMarks();
        }, 500);

        this.scanner.start();

        return this;
    }

    render() {
        $$("toolbarLabel").setValue(this.LABELS.formTitle);

        const formElements = [
            {
                view: "toolbar",
                borderless: true,
                elements: [
                    {
                        view: "text",
                        id: this.NAMES.searchInput,
                        placeholder: this.LABELS.search,
                        width: 200,
                        on: {
                            onTimedKeyPress: () => this._onSearch()
                        }
                    },
                    {
                        view: "button",
                        value: "Поиск",
                        width: 90,
                        click: () => this._onSearch()
                    },
                    {
                        view: "richselect",
                        id: this.NAMES.productGroupFilter,
                        label: this.LABELS.productGroupFilter,
                        labelWidth: 60,
                        width: 240,
                        value: this.ALL_GROUPS,
                        options: this.productGroupOptions,
                        on: {
                            onChange: () => this._onProductGroupChange()
                        }
                    },
                    {
                        view: "richselect",
                        id: this.NAMES.organisationFilter,
                        label: this.LABELS.organisationFilter,
                        labelWidth: 45,
                        width: 300,
                        value: this.ALL_INNS,
                        options: this.organisationOptions,
                        on: {
                            onChange: () => this._onOrganisationChange()
                        }
                    },
                    {
                        view: "checkbox",
                        id: this.NAMES.expiredFilter,
                        label: this.LABELS.expiredFilter,
                        labelWidth: 95,
                        width: 160,
                        tooltip: this.LABELS.expiredTooltip,
                        checkValue: true,
                        uncheckValue: false,
                        on: {
                            onChange: () => this._onExpiredOnlyChange()
                        }
                    },
                    {
                        view: "richselect",
                        id: this.NAMES.circulationFilter,
                        label: this.LABELS.circulationFilter,
                        labelWidth: 100,
                        width: 180,
                        value: this.CIRCULATION_ANY,
                        options: [
                            { id: this.CIRCULATION_ANY, value: this.LABELS.circulationAny },
                            { id: this.CIRCULATION_IN, value: this.LABELS.circulationIn },
                            { id: this.CIRCULATION_OUT, value: this.LABELS.circulationOut }
                        ],
                        on: {
                            onChange: () => this._onCirculationChange()
                        }
                    },
                    {},
                    {
                        view: "button",
                        id: this.NAMES.prevButton,
                        value: "←",
                        width: 50,
                        disabled: true,
                        click: () => this._goToPage(this.currentPage - 1)
                    },
                    {
                        view: "label",
                        id: this.NAMES.paginationInfo,
                        label: "",
                        width: 180,
                        align: "center"
                    },
                    {
                        view: "button",
                        id: this.NAMES.nextButton,
                        value: "→",
                        width: 50,
                        disabled: true,
                        click: () => this._goToPage(this.currentPage + 1)
                    }
                ]
            },
            this._createTable(),
            {}
        ];

        return {
            view: "form",
            id: this.id,
            name: this.formName,
            disabled: true,
            elements: formElements,
            on: {
                onAfterRender: () => {
                    this.scanner.start();
                    setTimeout(() => {
                        const searchInput = $$(this.NAMES.searchInput);
                        if (searchInput)
                            searchInput.focus();
                    }, 50);
                },
                onDestruct: () => {
                    this.scanner.stop();
                }
            }
        };
    }

    /// Колонка с экранированием HTML: в КИ бывают &, <, >.
    _textColumn(id, header, size) {
        return {
            id,
            header,
            sort: "string",
            ...size,
            template: (obj) => webix.template.escape(String(obj[id] ?? ""))
        };
    }

    _createTable() {
        return {
            view: "datatable",
            id: this.NAMES.marksTable,
            columns: [
                this._textColumn("sgtin", this.LABELS.sgtin, { fillspace: 2 }),
                this._textColumn("cis", this.LABELS.cis, { fillspace: 2 }),
                this._textColumn("status", this.LABELS.status, { width: 120 }),
                this._textColumn("circulation", this.LABELS.circulation, { width: 100 }),
                this._textColumn("expireDate", this.LABELS.expireDate, { width: 140 }),
                this._textColumn("ownerInn", this.LABELS.ownerInn, { width: 120 }),
                this._textColumn("productGroup", this.LABELS.productGroup, { fillspace: 1.5, minWidth: 180 }),
                this._textColumn("infoLoadedAt", this.LABELS.infoLoadedAt, { width: 160 }),
                {
                    id: "sourceDocumentId",
                    header: this.LABELS.sourceDocumentId,
                    sort: "string",
                    fillspace: 1,
                    template: obj => this._documentCell(obj.sourceDocumentId)
                }
            ],
            autoheight: true,
            scroll: false,
            select: false,
            on: {
                onItemClick: (rowId, event) => this._onDocumentClick(rowId, event)
            },
            data: []
        };
    }

    /// Документ-источник марки: у загруженного остатка это «stock», открывать нечего.
    _documentCell(value) {
        const documentId = String(value ?? "");
        if (!documentId)
            return "";

        const text = webix.template.escape(documentId);
        if (documentId === this.STOCK_SOURCE_ID)
            return text;

        return `<span class="gis-mt-document-link" style="color:#3d8fe0;cursor:pointer;text-decoration:underline">${text}</span>`;
    }

    _onDocumentClick(rowId, event) {
        const className = String(event?.target?.className ?? "");
        if (!className.includes("gis-mt-document-link"))
            return;

        if (event?.preventDefault)
            event.preventDefault();

        const table = $$(this.NAMES.marksTable);
        const row = table ? table.getItem(rowId) : null;
        const documentId = row?.sourceDocumentId;

        if (!documentId || documentId === this.STOCK_SOURCE_ID)
            return;

        import("./gisMtDocumentWindow.js").then(module => module.default(documentId));
    }

    /// Собирает списки фильтров из настроек организаций: товарные группы и ИНН.
    async _loadFilterOptions() {
        try {
            await loadProductGroupLabels();
            const config = await loadParameters();
            const groups = new Set();
            const organisations = new Map();

            const printGroups = config?.organisationConfig?.printGroups ?? [];
            for (const org of printGroups) {
                const orgGroups = org.trueApiIntegrationSettings?.productGroups ?? [];
                for (const group of orgGroups) {
                    if (group)
                        groups.add(group);
                }

                const inn = (org.inn ?? "").trim();
                if (!inn)
                    continue;

                const names = organisations.get(inn) ?? [];
                const name = (org.name ?? "").trim();
                if (name && !names.includes(name))
                    names.push(name);
                organisations.set(inn, names);
            }

            this.productGroupOptions = [
                { id: this.ALL_GROUPS, value: "Все группы" },
                ...[...groups]
                    .sort((a, b) => productGroupLabel(a).localeCompare(productGroupLabel(b), "ru"))
                    .map(g => ({ id: g, value: productGroupLabel(g) }))
            ];

            this.organisationOptions = [
                { id: this.ALL_INNS, value: this.LABELS.organisationAll },
                ...[...organisations.keys()]
                    .sort((a, b) => a.localeCompare(b))
                    .map(inn => ({ id: inn, value: this._organisationLabel(inn, organisations.get(inn)) }))
            ];

            this.filtersLoading = true;
            try {
                this._applyOptions(this.NAMES.productGroupFilter, this.productGroupOptions, this.ALL_GROUPS);
                this._applyOptions(this.NAMES.organisationFilter, this.organisationOptions, this.ALL_INNS);
            } finally {
                this.filtersLoading = false;
            }
        } catch (error) {
            console.error("Ошибка загрузки списка фильтров:", error);
        }
    }

    /// Подпись ИНН: сам ИНН и самое короткое название фирмы, чтобы варианты с одним ИНН не дублировались.
    _organisationLabel(inn, names) {
        if (!names || names.length === 0)
            return inn;

        const shortest = [...names].sort((a, b) => a.length - b.length)[0];
        const name = shortest.length > 28 ? `${shortest.slice(0, 28)}…` : shortest;
        const rest = names.length > 1 ? ` +${names.length - 1}` : "";

        return `${inn} — ${name}${rest}`;
    }

    _applyOptions(name, options, value) {
        const filter = $$(name);
        if (!filter)
            return;

        filter.define("options", options);
        filter.setValue(value);
        filter.refresh();
    }

    /// Возвращает выбранный ИНН или пустую строку для «Все ИНН».
    _selectedOrganisation() {
        const filter = $$(this.NAMES.organisationFilter);
        if (!filter)
            return "";

        const value = filter.getValue();
        if (!value || value === this.ALL_INNS)
            return "";

        return value;
    }

    /// Возвращает выбранную группу или пустую строку для «Все группы».
    _selectedProductGroup() {
        const filter = $$(this.NAMES.productGroupFilter);
        if (!filter)
            return "";

        const value = filter.getValue();
        if (!value || value === this.ALL_GROUPS)
            return "";

        return value;
    }

    _onProductGroupChange() {
        if (this.filtersLoading)
            return;

        this.productGroup = this._selectedProductGroup();
        this.currentPage = 1;
        this._loadMarks();
    }

    _onOrganisationChange() {
        if (this.filtersLoading)
            return;

        this.organisationInn = this._selectedOrganisation();
        this.currentPage = 1;
        this._loadMarks();
    }

    _onExpiredOnlyChange() {
        const filter = $$(this.NAMES.expiredFilter);
        this.expiredOnly = !!(filter && filter.getValue());
        this.currentPage = 1;
        this._loadMarks();
    }

    _onCirculationChange() {
        const filter = $$(this.NAMES.circulationFilter);
        const value = filter ? filter.getValue() : this.CIRCULATION_ANY;

        this.circulation = value === this.CIRCULATION_IN || value === this.CIRCULATION_OUT
            ? value
            : this.CIRCULATION_ANY;
        this.currentPage = 1;
        this._loadMarks();
    }

    _toSgtin(markCode) {
        let code = (markCode || "").trim().replace(/\\u001d/gi, "\x1d");

        if (code.startsWith("01")) {
            const gsPos = code.indexOf("\x1d");
            if (gsPos > 0) {
                const gtin = code.substring(2, 16);
                const serial = code.substring(18, gsPos);
                return gtin + serial;
            }

            // КИ без GS: убираем AI 01, оставляем GTIN + хвост до криптохвоста
            if (code.length >= 16) {
                let body = code.substring(2);
                const dash = body.indexOf("-");
                if (dash > 0)
                    body = body.substring(0, dash);
                return body;
            }
        }

        if (code.length === 29)
            return code.substring(0, 21);

        return code;
    }

    _onScan(code, meta = {}) {
        const sgtin = this._toSgtin(code);
        const searchInput = $$(this.NAMES.searchInput);

        if (searchInput) {
            searchInput.setValue(sgtin);
            searchInput.focus();
        }

        const warnings = [];
        if (meta.capsLock)
            warnings.push("Включён Caps Lock — раскладка сканера может исказить код");
        if (meta.cyrillic)
            warnings.push("В штрихкоде есть русские символы — проверьте раскладку клавиатуры");

        if (warnings.length > 0) {
            webix.message({
                text: warnings.join(". "),
                type: "error",
                expire: 5000
            });
        }

        this.searchTerm = sgtin;
        this.currentPage = 1;
        this._loadMarks();
    }

    _onSearch() {
        const searchInput = $$(this.NAMES.searchInput);
        if (searchInput) {
            this.searchTerm = searchInput.getValue() || "";
            searchInput.focus();
        }

        this.productGroup = this._selectedProductGroup();
        this.organisationInn = this._selectedOrganisation();
        this.currentPage = 1;
        this._loadMarks();
    }

    _goToPage(page) {
        if (page < 1)
            return;
        this.currentPage = page;
        this._loadMarks();
    }

    async _loadMarks() {
        const form = $$(this.id);
        if (!form)
            return;

        webix.extend(form, webix.ProgressBar);
        form.showProgress({ type: "icon" });

        const url = new URL(this.marksApiAddress, window.location.origin);
        url.searchParams.set("page", this.currentPage.toString());
        url.searchParams.set("pageSize", this.pageSize.toString());
        if (this.searchTerm)
            url.searchParams.set("search", this.searchTerm);
        if (this.productGroup)
            url.searchParams.set("productGroup", this.productGroup);
        if (this.organisationInn)
            url.searchParams.set("organisationInn", this.organisationInn);
        if (this.expiredOnly)
            url.searchParams.set("expiredOnly", "true");
        if (this.circulation === this.CIRCULATION_IN)
            url.searchParams.set("sold", "false");
        if (this.circulation === this.CIRCULATION_OUT)
            url.searchParams.set("sold", "true");

        let data;
        try {
            const response = await fetch(url);
            if (!response.ok)
                throw new Error(`Ошибка получения данных ${response.status}`);
            data = await response.json();
        } catch (error) {
            form.hideProgress();
            if (error.name === "TypeError" || String(error.message).includes("fetch"))
                return;
            webix.message({ text: "Ошибка при загрузке остатка марок ГИС МТ", type: "error" });
            return;
        }

        if (!data) {
            form.hideProgress();
            return;
        }

        await loadProductGroupLabels();
        this._updateTable(data);
        this._updatePagination(data);
        form.enable();
        form.hideProgress();
    }

    _updateTable(data) {
        const table = $$(this.NAMES.marksTable);
        if (!table)
            return;

        const rows = (data.marks || []).map(mark => ({
            id: mark.id || mark.sGtin || mark.cis,
            cis: mark.cis,
            sgtin: mark.sGtin || mark.id,
            status: mark.status,
            circulation: mark.sold ? "Нет" : "Да",
            expireDate: mark.expireDate ? new Date(mark.expireDate).toLocaleDateString() : "",
            ownerInn: mark.ownerInn,
            productGroup: productGroupLabel(mark.productGroup, mark.productGroupId),
            infoLoadedAt: mark.infoLoadedAt ? new Date(mark.infoLoadedAt).toLocaleString() : "",
            sourceDocumentId: mark.sourceDocumentId
        }));

        table.clearAll();
        table.parse(rows);
        table.resize();
    }

    _updatePagination(data) {
        const totalPages = data.totalPages || 1;
        const currentPage = data.currentPage || 1;
        const count = data.count || 0;

        const prevButton = $$(this.NAMES.prevButton);
        const nextButton = $$(this.NAMES.nextButton);
        const paginationInfo = $$(this.NAMES.paginationInfo);

        if (paginationInfo)
            paginationInfo.setValue(`${this.LABELS.page}${currentPage} / ${totalPages} (${count})`);

        if (prevButton) {
            prevButton.enable();
            if (currentPage <= 1)
                prevButton.disable();
        }

        if (nextButton) {
            nextButton.enable();
            if (currentPage >= totalPages)
                nextButton.disable();
        }
    }
}

export default function (id) {
    return new GisMtMarksView(id)
        .loadConfig()
        .delayedDataLoading()
        .render();
}
