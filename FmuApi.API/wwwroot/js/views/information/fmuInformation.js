import { TableToolabr, Label, TextBox, PasswordBox, TextBoxFormated, CheckBox } from "../../utils/ui.js";
import { ApiServerAdres } from '../../utils/net.js';

export function informationView(id) {
    $$("toolbarLabel").setValue("FMU-API: Информация");

    let appVersion = "loading...";

    webix.ajax().get(ApiServerAdres("/configuration/about"))
        .then(function (data) {
            appVersion = data.text();
            $$("appVersion").setValue(appVersion);
        });

    return {
        view: "form",
        id: id,
        name: "fmuInformationForm",
        elements:
            [
                {
                    view: "label",
                    id: "appVersion",
                    label: appVersion
                },
                {
                    cols: [
                        {
                            view: "label",
                            id: "lDatabaseEngineInfo",
                            label: "Для работы fmu-api необходимо установить Apache CouchDb: couchdb.apache.org.",
                            click: _ => window.open("https://couchdb.apache.org", "_blank").focus(),
                        },
                        {},
                    ],
                },
            ]
    }

}