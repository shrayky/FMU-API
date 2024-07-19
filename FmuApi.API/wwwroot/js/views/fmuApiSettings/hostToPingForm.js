import { TableToolabr, Label, TextBox, PasswordBox, CheckBox } from "../../utils/ui.js";

const formName = "HostToPingForm";

export function HostToPingForm(label, tableId, id) {
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
                TextBox("text", "название сайта без https, например: au124.ru", "HostName"),
                TextBox("text", "", "Id", {"hidden": true, "fomat": "1111"}),
                {
                    cols: [
                        {
                            view: "button",
                            value: "Добавить",
                            id: "addButton",
                            autowidth: "false",
                            width: 400,
                            click: _ => {
                                let siteAdres = $$("HostName").getValue();

                                if (siteAdres == undefined)
                                    return;

                                if (siteAdres == "")
                                    return;

                                let table = $$(tableId);

                                if (table == undefined)
                                    return;

                                let existRows = table.find(function (o) {
                                    return (o.value.toLowerCase() == siteAdres && o.id != id);
                                });

                                if (existRows.length > 0) {
                                    webix.message({
                                        text: "Этот сайт уже есть в списке!",
                                        type: "error"
                                    });
                                    return;
                                }

                                if (id == undefined) {
                                    let lastId = table.getLastId();
                                    let newId = lastId == undefined ? 1 : lastId + 1;
                                    table.add({ value: siteAdres, id: newId });
                                }
                                else {
                                    table.updateItem(id, { value: siteAdres, id: id });
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

        $$("HostName").setValue(item.value);
        $$("Id").setValue(item.id);
    }

    $$("HostName").focus();
}
