import { ScannerWedge } from '../../utils/scannerWedge.js';
import { loadParameters } from '../../services/ConfigurationService.js';

class GisMtMarksView {
    constructor(id) {
        this.formName = "GisMtMarksView";
        this.id = id;

        this.marksApiAddress = "/api/ts/gismt/marks";
        this.currentPage = 1;
        this.pageSize = 50;
        this.searchTerm = "";
        this.productGroup = "";
        this.ALL_GROUPS = "*";
        this.productGroupOptions = [{ id: "*", value: "Все группы" }];

        this.LABELS = {
            formTitle: "FMU-API: Остатки марок ГИС МТ",
            search: "Поиск по sGTIN / КИ",
            productGroupFilter: "Группа",
            cis: "КИ",
            sgtin: "sGTIN",
            status: "Статус",
            sold: "Продана",
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
            await this._loadProductGroups();
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
                        width: 280,
                        on: {
                            onTimedKeyPress: () => this._onSearch()
                        }
                    },
                    {
                        view: "richselect",
                        id: this.NAMES.productGroupFilter,
                        label: this.LABELS.productGroupFilter,
                        labelWidth: 60,
                        width: 260,
                        value: this.ALL_GROUPS,
                        options: this.productGroupOptions,
                        on: {
                            onChange: () => this._onProductGroupChange()
                        }
                    },
                    {
                        view: "button",
                        value: "Поиск",
                        width: 100,
                        click: () => this._onSearch()
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

    _createTable() {
        return {
            view: "datatable",
            id: this.NAMES.marksTable,
            columns: [
                { id: "sgtin", header: this.LABELS.sgtin, fillspace: 2, sort: "string" },
                { id: "cis", header: this.LABELS.cis, fillspace: 2, sort: "string" },
                { id: "status", header: this.LABELS.status, width: 120, sort: "string" },
                { id: "sold", header: this.LABELS.sold, width: 90, sort: "string" },
                { id: "expireDate", header: this.LABELS.expireDate, width: 140, sort: "string" },
                { id: "ownerInn", header: this.LABELS.ownerInn, width: 120, sort: "string" },
                { id: "productGroup", header: this.LABELS.productGroup, width: 100, sort: "string" },
                { id: "infoLoadedAt", header: this.LABELS.infoLoadedAt, width: 160, sort: "string" },
                { id: "sourceDocumentId", header: this.LABELS.sourceDocumentId, fillspace: 1, sort: "string" }
            ],
            autoheight: true,
            scroll: false,
            select: false,
            data: []
        };
    }

    /// Собирает уникальный список товарных групп из настроек организаций.
    async _loadProductGroups() {
        try {
            const config = await loadParameters();
            const groups = new Set();

            const printGroups = config?.organisationConfig?.printGroups ?? [];
            for (const org of printGroups) {
                const orgGroups = org.trueApiIntegrationSettings?.productGroups ?? [];
                for (const group of orgGroups) {
                    if (group)
                        groups.add(group);
                }
            }

            this.productGroupOptions = [
                { id: this.ALL_GROUPS, value: "Все группы" },
                ...[...groups].sort().map(g => ({ id: g, value: g }))
            ];

            const filter = $$(this.NAMES.productGroupFilter);
            if (filter) {
                filter.define("options", this.productGroupOptions);
                filter.setValue(this.ALL_GROUPS);
                filter.refresh();
            }
        } catch (error) {
            console.error("Ошибка загрузки списка товарных групп:", error);
        }
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
        this.productGroup = this._selectedProductGroup();
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
            sold: mark.sold ? "Да" : "Нет",
            expireDate: mark.expireDate ? new Date(mark.expireDate).toLocaleDateString() : "",
            ownerInn: mark.ownerInn,
            productGroup: mark.productGroup,
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
