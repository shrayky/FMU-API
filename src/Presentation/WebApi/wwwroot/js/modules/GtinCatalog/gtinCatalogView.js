class GtinCatalogView {
    constructor(id) {
        this.id = id;
        this.formName = "GtinCatalogView";
        this.gtinApi = "/api/product-groups/gtin-catalog";
        this.currentPage = 1;
        this.pageSize = 50;
        this.searchTerm = "";

        this.LABELS = {
            formTitle: "FMU-API: Каталог GTIN",
            search: "Поиск по GTIN",
            gtin: "GTIN",
            trueApiGroupId: "Код ЧЗ",
            loadError: "Не удалось загрузить данные"
        };

        this.NAMES = {
            gtinTable: "gtinCatalogTable",
            gtinSearch: "gtinCatalogSearch",
            paginationInfo: "gtinCatalogPagination",
            prevButton: "gtinCatalogPrev",
            nextButton: "gtinCatalogNext"
        };
    }

    loadConfig() {
        return this;
    }

    delayedDataLoading() {
        setTimeout(() => this._loadGtinCatalog(), 300);
        return this;
    }

    render() {
        $$("toolbarLabel").setValue(this.LABELS.formTitle);

        return {
            view: "form",
            id: this.id,
            name: this.formName,
            elements: [
                this._toolbar(),
                this._table(),
                {}
            ]
        };
    }

    _toolbar() {
        return {
            view: "toolbar",
            borderless: true,
            elements: [
                {
                    view: "text",
                    id: this.NAMES.gtinSearch,
                    placeholder: this.LABELS.search,
                    width: 280,
                    on: {
                        onTimedKeyPress: () => this._onSearch()
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
                    width: 160,
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
        };
    }

    _table() {
        return {
            view: "datatable",
            id: this.NAMES.gtinTable,
            columns: [
                { id: "gtin", header: this.LABELS.gtin, fillspace: true, sort: "string" },
                { id: "trueApiGroupId", header: this.LABELS.trueApiGroupId, width: 110, sort: "int" }
            ],
            autoheight: true,
            scroll: false,
            select: false,
            data: []
        };
    }

    _onSearch() {
        const searchInput = $$(this.NAMES.gtinSearch);
        this.searchTerm = searchInput ? searchInput.getValue() : "";
        this.currentPage = 1;
        this._loadGtinCatalog();
    }

    _goToPage(page) {
        if (page < 1)
            return;

        this.currentPage = page;
        this._loadGtinCatalog();
    }

    async _loadGtinCatalog() {
        const url = new URL(this.gtinApi, window.location.origin);
        url.searchParams.set("page", this.currentPage.toString());
        url.searchParams.set("pageSize", this.pageSize.toString());
        if (this.searchTerm)
            url.searchParams.set("search", this.searchTerm);

        try {
            const response = await fetch(url);
            if (!response.ok)
                throw new Error(response.statusText);

            const data = await response.json();
            this._updateTable(data);
            this._updatePagination(data);
        } catch (error) {
            console.error(error);
            webix.message({ text: this.LABELS.loadError, type: "error" });
        }
    }

    _updateTable(data) {
        const table = $$(this.NAMES.gtinTable);
        if (!table)
            return;

        table.clearAll();
        table.parse((data.items || []).map(item => ({
            id: item.gtin || item.id,
            gtin: item.gtin,
            trueApiGroupId: item.trueApiGroupId
        })));
        table.resize();
    }

    _updatePagination(data) {
        const currentPage = data.currentPage || 1;
        const totalPages = data.totalPages || 1;
        const count = data.count || 0;

        const prevButton = $$(this.NAMES.prevButton);
        const nextButton = $$(this.NAMES.nextButton);
        const paginationInfo = $$(this.NAMES.paginationInfo);

        if (paginationInfo)
            paginationInfo.setValue(`${currentPage} / ${totalPages} (${count})`);

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
    return new GtinCatalogView(id)
        .loadConfig()
        .delayedDataLoading()
        .render();
}
