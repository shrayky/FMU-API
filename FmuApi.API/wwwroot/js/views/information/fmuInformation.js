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
                            label: "Для работы fmu-api необходимо скачать и установить базу данных <a href=\"https://couchdb.apache.org\" target=\"_blank\" style=\"color: red\">Apache CouchDb.</a>",
                        },
                        {},
                    ],
                },
                {
                    view: "label",
                    id: "browswerRecomendation",
                    label: "Для работы рекомендуется использовать браузер Vivaldi, Edge, Opera, Chrome - в общем все на базе Chromium."
                }
            ]
    }

}