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
            addForm: "pgMappingAddForm"
        };
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
                }
            ],
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
                checkSmp: !!row.checkSmp
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

        const saved = await this._saveMapping({
            atolCode: Number(item.atolCode),
            trueApiGroupId: Number(item.trueApiGroupId),
            name: item.name || "",
            checkSmp: !!item.checkSmp
        });

        if (!saved)
            this._loadMapping();
    }

    async _onCheckChanged(rowId, colId, state) {
        if (colId !== "checkSmp")
            return;

        const table = $$(this.NAMES.mappingTable);
        const item = table.getItem(rowId);
        if (!item)
            return;

        item.checkSmp = !!state;
        table.updateItem(rowId, item);

        const saved = await this._saveMapping({
            atolCode: Number(item.atolCode),
            trueApiGroupId: Number(item.trueApiGroupId),
            name: item.name || "",
            checkSmp: !!state
        });

        if (!saved)
            this._loadMapping();
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
            width: 420,
            head: this.LABELS.addTitle,
            body: {
                view: "form",
                id: this.NAMES.addForm,
                elements: [
                    { view: "text", name: "atolCode", label: this.LABELS.atolCode, labelWidth: 120 },
                    { view: "text", name: "trueApiGroupId", label: this.LABELS.trueApiGroupId, labelWidth: 120 },
                    { view: "text", name: "name", label: this.LABELS.name, labelWidth: 120 },
                    { view: "checkbox", name: "checkSmp", label: this.LABELS.checkSmp, labelWidth: 120, checkValue: true, uncheckValue: false },
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
        const entity = {
            atolCode: Number(values.atolCode),
            trueApiGroupId: Number(values.trueApiGroupId),
            name: values.name || "",
            checkSmp: !!values.checkSmp
        };

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
