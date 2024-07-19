const formName = "OrganisationForm";

export function OrganisationForm(label, tableId, id) {
    let windowInnerWidth = window.innerWidth;
    let windowInnerHeight = window.innerHeight;

    webix.ui({
        view: "window",
        container: "window",
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
                {
                    view: "text",
                    type: "number",
                    label: "Код организации (группы печати), если группы печати не используются, то все равно код должен быть 1",
                    format: "1111",
                    labelPosition: "top",
                    id: "OrganisationId",
                    name: "organisationId"
                },

                {
                    view: "text",
                    label: "ИНН организации (не обязательно)",
                    labelPosition: "top",
                    id: "OrganisationInn",
                    name: "organisationInn"
                },

                {
                    view: "text",
                    label: "X-API key (обязательно)",
                    labelPosition: "top",
                    id: "XAPIKEY",
                    name: "xapikey"
                },

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
                                    console.log(obj);
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
        $$("OrganisationId").setValue(table.getLastId() + 1);
    }
}
