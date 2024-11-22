import { TableToolabr, Label, TextBox, PasswordBox, CheckBox } from "../../utils/ui.js";

const formName = "OrganisationForm";

export function OrganisationForm(label, tableId, id) {
    const windowInnerWidth = window.innerWidth;
    const windowInnerHeight = window.innerHeight;

    webix.ui({
        view: "window",
        id: formName,
        position: "center",
        modal: true,
        move: false,
        resize: false,
        width: windowInnerWidth * 0.4,

        head: {
            view: "toolbar",
            elements: [
                {
                    view: "label",
                    label: label,
                },

                {
                    view: "icon",
                    icon: "wxi-close",
                    click: _ => $$(formName).close()
                }
            ]
        },

        body: {
            rows: [
                TextBox("number", "Код организации (группы печати), если группы печати не используются, то все равно код должен быть 1", "OrganisationId", {"format": "1111"}),
                TextBox("text", "ИНН организации (не обязательно)", "OrganisationInn"),
                TextBox("text", "X-API key (обязательно)", "XAPIKEY"),

                {
                    cols: [
                        {
                            view: "button",
                            value: "Добавить",
                            id: "addButton",
                            autowidth: "false",
                            width: 400,
                            click: _ => {
                                let organisationId = $$("OrganisationId").getValue();

                                if (organisationId == "")
                                    return;

                                let table = $$(tableId);

                                if (table == undefined)
                                    return;

                                let existRows = table.find(function (obj) {
                                    return (obj.id == organisationId && organisationId != id);
                                });

                                if (existRows.length > 0) {
                                    webix.message({
                                        text: "Организация с таким кодом уже есть в списке!",
                                        type: "error"
                                    });
                                    return;
                                }

                                if (id == undefined) {
                                    table.add({
                                        id: organisationId,
                                        xapikey: $$("XAPIKEY").getValue(),
                                        inn: $$("OrganisationInn").getValue()
                                    });
                                }
                                else {
                                    table.updateItem(id, {
                                        id: organisationId,
                                        xapikey: $$("XAPIKEY").getValue(),
                                        inn: $$("OrganisationInn").getValue()
                                    });
                                }

                                $$(formName).close();
                            }
                        },

                        {
                            view: "button",
                            value: "Закрыть",
                            id: "closeBtn",
                            autowidth: "false",
                            width: 400,
                            click: _ => $$(formName).close()
                        },
                        {}
                    ]
                }

            ]
        }

    }).show();

    let table = $$(tableId);

    if (id != undefined) {
        let item = table.getItem(id);

        $$("OrganisationId").setValue(item.id);
        $$("OrganisationId").disable();

        $$("OrganisationInn").setValue(item.inn);
        $$("XAPIKEY").setValue(item.xapikey);
    }
    else {
        let lastId = table.getLastId();
        $$("OrganisationId").setValue(lastId == undefined ? 1 : lastId + 1);
    }
}
