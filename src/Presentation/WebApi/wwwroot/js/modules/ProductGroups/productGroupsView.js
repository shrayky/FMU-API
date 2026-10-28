class ProductGroupsView {
    constructor(id) {
        this.id = id;
        this.formName = "ProductGroupsView";
        this.mappingApi = "/api/product-groups/mapping";

        this.LABELS = {
            formTitle: "FMU-API: Товарные группы",
            atolCode: "Код Атол",
            trueApiGroupId: "Код ЧЗ",
            name: "Название",
            checkSmp: "ЕМЦ",
            checkMrp: "Контроль МРЦ",
            checkExpireDate: "Срок годности!",
            haveExpireDate: "Есть срок годности",
            forbidSaleByExpireDate: "Запрет продажи по сроку",
            help: "?",
            helpTitle: "Флажки товарных групп",
            add: "Добавить",
            remove: "Удалить",
            refresh: "Обновить",
            noRowSelected: "Выберите строку",
            deleteConfirm: "Удалить маппинг кода Атол {0}?",
            saveError: "Не удалось сохранить маппинг",
            deleteError: "Не удалось удалить маппинг",
            loadError: "Не удалось загрузить данные",
            addTitle: "Новый маппинг",
            save: "Сохранить",
            cancel: "Отмена"
        };

        this.NAMES = {
            mappingTable: "pgMappingTable",
            addWindow: "pgMappingAddWindow",
            addForm: "pgMappingAddForm",
            helpWindow: "pgMappingHelpWindow"
        };

        this.FLAG_COLUMNS = ["checkSmp", "checkMrp", "checkExpireDate", "haveExpireDate", "forbidSaleByExpireDate"];

        webix.html.addStyle(
            "#pgMappingTable .webix_ss_header .webix_hcell{line-height:48px;}" +
            ".pg-header-twoline{display:flex;flex-direction:column;justify-content:center;" +
            "line-height:18px !important;white-space:normal;text-align:center;height:48px;box-sizing:border-box;}" +
            ".pg-help .webix_template{padding:14px 16px;line-height:1.45;}" +
            ".pg-help-item{margin-bottom:16px;}" +
            ".pg-help-title{font-weight:600;}" +
            ".pg-help-when{margin:2px 0 4px;opacity:0.75;}",
            "pg-header-twoline"
        );
    }

    loadConfig() {
        return this;
    }

    delayedDataLoading() {
        setTimeout(() => this._loadMapping(), 300);
        return this;
    }

    render() {
        $$("toolbarLabel").setValue(this.LABELS.formTitle);

        return {
            view: "form",
            id: this.id,
            name: this.formName,
            elements: [
                this._mappingToolbar(),
                this._mappingTable(),
                {}
            ]
        };
    }

    _mappingToolbar() {
        return {
            view: "toolbar",
            borderless: true,
            elements: [
                {
                    view: "button",
                    value: this.LABELS.add,
                    width: 120,
                    click: () => this._openAddWindow()
                },
                {
                    view: "button",
                    value: this.LABELS.remove,
                    width: 120,
                    click: () => this._deleteSelected()
                },
                {
                    view: "button",
                    value: this.LABELS.refresh,
                    width: 120,
                    click: () => this._loadMapping()
                },
                {
                    view: "button",
                    value: this.LABELS.help,
                    width: 40,
                    click: () => this._openHelpWindow()
                },
                {}
            ]
        };
    }

    _mappingTable() {
        return {
            view: "datatable",
            id: this.NAMES.mappingTable,
            editable: true,
            select: "row",
            columns: [
                { id: "atolCode", header: this.LABELS.atolCode, width: 110, sort: "int" },
                { id: "trueApiGroupId", header: this.LABELS.trueApiGroupId, width: 110, sort: "int", editor: "text" },
                { id: "name", header: this.LABELS.name, fillspace: true, sort: "string", editor: "text" },
                {
                    id: "checkSmp",
                    header: this.LABELS.checkSmp,
                    width: 70,
                    template: "{common.checkbox()}",
                    checkValue: true,
                    uncheckValue: false
                },
                {
                    id: "checkMrp",
                    header: { text: this.LABELS.checkMrp, css: "pg-header-twoline" },
                    width: 90,
                    css: { "text-align": "center" },
                    template: "{common.checkbox()}",
                    checkValue: true,
                    uncheckValue: false
                },
                {
                    id: "checkExpireDate",
                    header: { text: "Срок<br>годности!", css: "pg-header-twoline" },
                    width: 90,
                    css: { "text-align": "center" },
                    template: "{common.checkbox()}",
                    checkValue: true,
                    uncheckValue: false
                },
                {
                    id: "haveExpireDate",
                    header: { text: "Есть срок<br>годности", css: "pg-header-twoline" },
                    width: 90,
                    css: { "text-align": "center" },
                    template: "{common.checkbox()}",
                    checkValue: true,
                    uncheckValue: false
                },
                {
                    id: "forbidSaleByExpireDate",
                    header: { text: "Запрет продажи<br>по сроку", css: "pg-header-twoline" },
                    width: 120,
                    css: { "text-align": "center" },
                    template: "{common.checkbox()}",
                    checkValue: true,
                    uncheckValue: false
                }
            ],
            headerRowHeight: 56,
            autoheight: true,
            scroll: false,
            checkboxRefresh: true,
            data: [],
            on: {
                onAfterEditStop: (state, editor) => this._onMappingEdited(state, editor),
                onCheck: (rowId, colId, state) => this._onCheckChanged(rowId, colId, state)
            }
        };
    }

    async _loadMapping() {
        try {
            const response = await fetch(this.mappingApi);
            if (!response.ok)
                throw new Error(response.statusText);

            const rows = await response.json();
            const table = $$(this.NAMES.mappingTable);
            if (!table)
                return;

            table.clearAll();
            table.parse((rows || []).map(row => ({
                id: row.atolCode,
                atolCode: row.atolCode,
                trueApiGroupId: row.trueApiGroupId,
                name: row.name || "",
                checkSmp: !!row.checkSmp,
                checkMrp: !!row.checkMrp,
                checkExpireDate: !!row.checkExpireDate,
                haveExpireDate: !!row.haveExpireDate,
                forbidSaleByExpireDate: !!row.forbidSaleByExpireDate
            })));
            table.resize();
        } catch (error) {
            console.error(error);
            webix.message({ text: this.LABELS.loadError, type: "error" });
        }
    }

    async _onMappingEdited(state, editor) {
        if (state.value === state.old)
            return;

        const table = $$(this.NAMES.mappingTable);
        const item = table.getItem(editor.row);
        if (!item)
            return;

        const saved = await this._saveMapping(this._entityFromRow(item));

        if (!saved)
            this._loadMapping();
    }

    async _onCheckChanged(rowId, colId, state) {
        if (!this.FLAG_COLUMNS.includes(colId))
            return;

        const table = $$(this.NAMES.mappingTable);
        const item = table.getItem(rowId);
        if (!item)
            return;

        item[colId] = !!state;
        table.updateItem(rowId, item);

        const saved = await this._saveMapping(this._entityFromRow(item));

        if (!saved)
            this._loadMapping();
    }

    _entityFromRow(item) {
        return {
            atolCode: Number(item.atolCode),
            trueApiGroupId: Number(item.trueApiGroupId),
            name: item.name || "",
            checkSmp: !!item.checkSmp,
            checkMrp: !!item.checkMrp,
            checkExpireDate: !!item.checkExpireDate,
            haveExpireDate: !!item.haveExpireDate,
            forbidSaleByExpireDate: !!item.forbidSaleByExpireDate
        };
    }

    async _saveMapping(entity) {
        try {
            const response = await fetch(this.mappingApi, {
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(entity)
            });

            if (!response.ok) {
                webix.message({ text: this.LABELS.saveError, type: "error" });
                return false;
            }

            return true;
        } catch (error) {
            console.error(error);
            webix.message({ text: this.LABELS.saveError, type: "error" });
            return false;
        }
    }

    _openAddWindow() {
        if ($$(this.NAMES.addWindow))
            $$(this.NAMES.addWindow).close();

        webix.ui({
            view: "window",
            id: this.NAMES.addWindow,
            modal: true,
            position: "center",
            width: 480,
            head: this.LABELS.addTitle,
            body: {
                view: "form",
                id: this.NAMES.addForm,
                elements: [
                    { view: "text", name: "atolCode", label: this.LABELS.atolCode, labelWidth: 120 },
                    { view: "text", name: "trueApiGroupId", label: this.LABELS.trueApiGroupId, labelWidth: 120 },
                    { view: "text", name: "name", label: this.LABELS.name, labelWidth: 120 },
                    { view: "checkbox", name: "checkSmp", label: this.LABELS.checkSmp, labelWidth: 220, checkValue: true, uncheckValue: false },
                    { view: "checkbox", name: "checkMrp", label: this.LABELS.checkMrp, labelWidth: 220, checkValue: true, uncheckValue: false },
                    { view: "checkbox", name: "checkExpireDate", label: this.LABELS.checkExpireDate, labelWidth: 220, checkValue: true, uncheckValue: false },
                    { view: "checkbox", name: "haveExpireDate", label: this.LABELS.haveExpireDate, labelWidth: 220, checkValue: true, uncheckValue: false },
                    { view: "checkbox", name: "forbidSaleByExpireDate", label: this.LABELS.forbidSaleByExpireDate, labelWidth: 220, checkValue: true, uncheckValue: false },
                    {
                        cols: [
                            {
                                view: "button",
                                value: this.LABELS.save,
                                click: () => this._saveNewMapping()
                            },
                            {
                                view: "button",
                                value: this.LABELS.cancel,
                                click: () => $$(this.NAMES.addWindow).close()
                            }
                        ]
                    }
                ]
            }
        }).show();
    }

    async _saveNewMapping() {
        const form = $$(this.NAMES.addForm);
        const values = form.getValues();
        const entity = this._entityFromRow(values);

        if (!entity.atolCode || !entity.trueApiGroupId) {
            webix.message({ text: this.LABELS.saveError, type: "error" });
            return;
        }

        const saved = await this._saveMapping(entity);
        if (!saved)
            return;

        $$(this.NAMES.addWindow).close();
        this._loadMapping();
    }

    _openHelpWindow() {
        const id = this.NAMES.helpWindow;
        if ($$(id))
            $$(id).close();

        webix.ui({
            view: "window",
            id,
            modal: true,
            position: "center",
            width: 560,
            height: 640,
            head: {
                view: "toolbar",
                elements: [
                    { view: "label", label: this.LABELS.helpTitle },
                    {
                        view: "icon",
                        icon: "wxi-close",
                        click: () => $$(id).close()
                    }
                ]
            },
            body: {
                view: "template",
                scroll: "y",
                css: "pg-help",
                template: this._helpHtml()
            }
        }).show();
    }

    _helpHtml() {
        const items = [
            {
                title: "ЕМЦ",
                when: "Начало чека (begin) — перед добавлением оплаты",
                text: "Признак того, что у товара нужно контролировать ЕМЦ. Если цена ниже — отказ «цена ниже минимальной розничной». По умолчанию включён у табака и никотиносодержащей продукции."
            },
            {
                title: "Контроль МРЦ",
                when: "Начало чека (begin) — перед добавлением оплаты",
                text: "Включён: цена должна точно совпасть с МРЦ. По умолчанию включён у табака."
            },
            {
                title: "Срок годности!",
                when: "Начало чека (begin) — перед добавлением оплаты",
                text: "Нет срока годности у марки — отказ «отсутствует срок годности». Чтобы исключить продажу товара при офлайн-проверке."
            },
            {
                title: "Есть срок годности",
                when: "Проверка марки (check) — при сканировании марки",
                text: "Продажу не запрещает. Для Фронтола 29 и выше — признак того, что у этого товара надо показать сообщение «проверь срок годности» при офлайн-проверке."
            },
            {
                title: "Запрет продажи по сроку",
                when: "Начало чека (begin) — перед добавлением оплаты",
                text: "Срок уже истёк — отказ продажи. Пустой срок этим флажком не проверяется."
            }
        ];

        return items.map(item =>
            `<div class="pg-help-item">` +
            `<div class="pg-help-title">${item.title}</div>` +
            `<div class="pg-help-when">${item.when}</div>` +
            `<div>${item.text}</div>` +
            `</div>`
        ).join("");
    }

    async _deleteSelected() {
        const table = $$(this.NAMES.mappingTable);
        const selected = table.getSelectedItem();
        if (!selected) {
            webix.message(this.LABELS.noRowSelected);
            return;
        }

        webix.confirm({
            text: this.LABELS.deleteConfirm.replace("{0}", selected.atolCode),
            ok: this.LABELS.remove,
            cancel: this.LABELS.cancel,
            callback: async (result) => {
                if (!result)
                    return;

                try {
                    const response = await fetch(`${this.mappingApi}/${selected.atolCode}`, {
                        method: "DELETE"
                    });

                    if (!response.ok) {
                        webix.message({ text: this.LABELS.deleteError, type: "error" });
                        return;
                    }

                    this._loadMapping();
                } catch (error) {
                    console.error(error);
                    webix.message({ text: this.LABELS.deleteError, type: "error" });
                }
            }
        });
    }
}

export default function (id) {
    return new ProductGroupsView(id)
        .loadConfig()
        .delayedDataLoading()
        .render();
}
