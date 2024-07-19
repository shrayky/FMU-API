import { TableToolabr, Label, TextBox, PasswordBox, TextBoxFormated, CheckBox } from "../../utils/ui.js";

export function informationView(id) {
    $$("toolbarLabel").setValue("FMU-API: Информация");

    return {
        view: "form",
        id: id,
        name: "fmuInformationForm",
        elements:
        [
            {
            cols: [
            {       
                view: "label",
                id: "lDatabaseEngineInfo1",
                label: "Для работы fmu-api необходимо установить Apache CouchDb: couchdb.apache.org.",
                click: _ => window.open("https://couchdb.apache.org", "_blank").focus(),
            },
            {}
            ],
        }
        ]
    }

}