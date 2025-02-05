import { Label, TableToolabr, Text, Number, padding } from "../../../utils/ui.js";

class OrganisationsConfigurationElement {
    constructor(id) {
        this.id = id;
        this.formName = "OrganisationForm";
        this.LABELS = {
            title: "Организации",
            newOrg: "Новая организация",
            editOrg: "Организация",
            code: "Код организации (группы печати), если группы печати не используются, то все равно код должен быть 1",
            inn: "ИНН организации",
            xapikey: "X-API key",
            add: "Сохранить",
            close: "Закрыть"
        };
    }

    loadConfig(config) {
        if (config && config.organisationConfig && config.organisationConfig.printGroups) {
           this.printGroups = config.organisationConfig.printGroups;
        }

        return this;
    }

    render() {
        var elements = [];

        elements.push(
            Label("lOrganizations", this.LABELS.title)
        );

        elements.push({
            padding: padding,
            name: "organisationConfig",
            rows: [
                TableToolabr("PrintGroups"),
                this._createFormTable()
            ]
        });

        return { id: this.id, rows: elements };
    }

    _createFormTable() {
        return {
            view: "formtable",
            id: "PrintGroups",
            name: "organisationConfig.printGroups",
            data: this.printGroups,
            resizeColumn: true,
            resizeRow: true,
            select: true,
            minHeight: 250,
            columns: [
                { id: "id", header: "Код" },
                { id: "inn", header: "ИНН", fillspace: true },
                { id: "xapikey", header: "XAPIKEY", fillspace: true },
            ],
            on: {
                onAfterSelect: (selection) => {
                    $$("delete_PrintGroups").enable();
                },
                onAfterDelete: (id) => {
                    $$("delete_PrintGroups").disable();
                    if ($$("PrintGroups").count() == 0)
                        $$("deleteAll_PrintGroups").disable();
                },
                onBeforeAdd: (id, obj) => {
                    if (obj.xapikey == undefined) {
                        this.showForm(this.LABELS.newOrg, "PrintGroups");
                        return false;
                    }
                },
                onItemDblClick: (id) => {
                    this.showForm(this.LABELS.editOrg, "PrintGroups", id);
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
                Number(this.LABELS.code, "OrganisationId", "1111"),
                Text(this.LABELS.inn, "OrganisationInn"),
                Text(this.LABELS.xapikey, "XAPIKEY"),
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
        let organisationId = $$("OrganisationId").getValue();
        if (organisationId == "") return;

        let table = $$(tableId);
        if (table == undefined) return;

        let existRows = table.find(obj => obj.id == organisationId && organisationId != id);
        if (existRows.length > 0) {
            webix.message({
                text: "Организация с таким кодом уже есть в списке!",
                type: "error"
            });
            return;
        }

        const newData = {
            id: organisationId,
            xapikey: $$("XAPIKEY").getValue(),
            inn: $$("OrganisationInn").getValue()
        };

        if (id == undefined) {
            table.add(newData);
        } else {
            table.updateItem(id, newData);
        }

        if (table.count() > 0)
            $$("deleteAll_PrintGroups").enable();

        $$(this.formName).close();
    }

    _initFormValues(tableId, id) {
        let table = $$(tableId);

        if (id != undefined) {
            let item = table.getItem(id);
            $$("OrganisationId").setValue(item.id);
            $$("OrganisationId").disable();
            $$("OrganisationInn").setValue(item.inn);
            $$("XAPIKEY").setValue(item.xapikey);
        } else {
            let lastId = table.getLastId();
            $$("OrganisationId").setValue(lastId == undefined ? 1 : lastId + 1);
        }
    }
}

export default function (id, config) {
    return new OrganisationsConfigurationElement(id)
        .loadConfig(config)
        .render();
}