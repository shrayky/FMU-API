import { Label, TableToolbar, Text, Number, padding } from "../../../utils/ui.js";

class CheckInternetConnectionElement {
    constructor(id) {
        this.id = id;
        this.formName = "HostToPingForm";
        this.LABELS = {
            title: "Адреса сайтов для проверки доступности интернета (если проверка не нужна, то список должен быть пустым)",
            newHost: "Новый сайт",
            editHost: "Сайт",
            hostNameHint: "название сайта без https, например: au124.ru",
            add: "Сохранить",
            close: "Закрыть",
            existError: "Этот сайт уже есть в списке!"
        };
    }

    loadConfig(config) {
        if (config && config.hostsToPing) {
            this.hostsToPing = config.hostsToPing;
        }
        return this;
    }

    render() {
        var elements = [];

        elements.push(
            Label("lHostsToPing", this.LABELS.title)
        );

        elements.push({
            padding: padding,
            rows: [
                TableToolbar("HostsToPing"),
                this._createFormTable()
            ]
        });

        return { id: this.id, rows: elements };
    }

    _createFormTable() {
        return {
            view: "formtable",
            id: "HostsToPing",
            name: "hostsToPing",
            data: this.hostsToPing,
            resizeColumn: true,
            resizeRow: true,
            select: true,
            minHeight: 250,
            columns: [
                { id: "id", header: "Код", hidden: true },
                { id: "value", header: "Хост", fillspace: true },
            ],
            on: {
                onAfterSelect: () => {
                    $$("delete_HostsToPing").enable();
                },
                onAfterDelete: () => {
                    $$("delete_HostsToPing").disable();
                    if ($$("HostsToPing").count() == 0) {
                        $$("deleteAll_HostsToPing").disable();
                    }
                },
                onBeforeAdd: (id, obj) => {
                    if (obj.value == undefined) {
                        this.showForm(this.LABELS.newHost, "HostsToPing");
                        return false;
                    }
                },
                onItemDblClick: (id) => {
                    this.showForm(this.LABELS.editHost, "HostsToPing", id);
                }
            }
        };
    }

    showForm(label, tableId, id) {
        const windowInnerWidth = window.innerWidth;

        webix.ui({
            view: "window",
            id: this.formName,
            position: "center",
            modal: true,
            move: false,
            resize: false,
            width: windowInnerWidth * 0.8,
            head: this._createFormHeader(label),
            body: this._createFormBody(tableId, id)
        }).show();

        this._initFormValues(tableId, id);
        $$("HostName").focus();
    }

    _createFormHeader(label) {
        return {
            view: "toolbar",
            elements: [
                {
                    view: "label",
                    label: label,
                },
                {
                    view: "icon",
                    icon: "wxi-close",
                    click: () => $$(this.formName).close()
                }
            ]
        };
    }

    _createFormBody(tableId, id) {
        return {
            rows: [
                Text(this.LABELS.hostNameHint, "HostName", ""),
                Number("id hidden", "Id", "", "1111", {"hidden": true}),
                {
                    cols: [
                        {
                            view: "button",
                            value: this.LABELS.add,
                            id: "addButton",
                            autowidth: "false",
                            width: 400,
                            click: () => this._handleAddButton(tableId, id)
                        },
                        {
                            view: "button",
                            value: this.LABELS.close,
                            id: "closeBtn",
                            autowidth: "false",
                            width: 400,
                            click: () => $$(this.formName).close()
                        },
                        {}
                    ]
                }
            ]
        };
    }

    _handleAddButton(tableId, id) {
        let siteAdres = $$("HostName").getValue();
        if (!siteAdres) return; 

        let table = $$(tableId);
        if (!table) return;

        let existRows = table.find(o => o.value.toLowerCase() == siteAdres && o.id != id);
        if (existRows.length > 0) {
            webix.message({
                text: this.LABELS.existError,
                type: "error"
            });
            return;
        }

        if (id == undefined) {
            let lastId = table.getLastId();
            let newId = lastId == undefined ? 1 : lastId + 1;
            table.add({ value: siteAdres, id: newId });
        } else {
            table.updateItem(id, { value: siteAdres, id: id });
        }

        if (table.count() > 0)
            $$("deleteAll_HostsToPing").enable();

        $$(this.formName).close();
    }

    _initFormValues(tableId, id) {
        if (id != undefined) {
            let table = $$(tableId);
            let item = table.getItem(id);
            $$("HostName").setValue(item.value);
            $$("Id").setValue(item.id);
        }
    }
}

export default function(id, config) {
    return new CheckInternetConnectionElement(id)
        .loadConfig(config)
        .render();
}