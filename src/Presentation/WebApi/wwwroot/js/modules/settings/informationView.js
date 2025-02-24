import { ApiServerAdres } from '../../utils/net.js';

export default function informationView(id) {
    $$("toolbarLabel").setValue("FMU-API: Информация о системе");

    let appVersion = "Версия загружается...";

    return {
        id,
        rows: [
            {
                view: "form",
                on:{
                    onViewShow: init(),
                },
                elements: [
                    {
                        view: "label",
                        id: "appVersion",
                        label: appVersion
                    },
                    {
                        view: "label",
                        id: "lDatabaseEngineInfo",
                        label: "Для работы fmu-api необходимо скачать и установить базу данных <a href=\"https://couchdb.apache.org\" target=\"_blank\" style=\"color: red\">Apache CouchDb.</a>",
                    },
                    {
                        view: "label",
                        id: "browserRecommendation",
                        label: "Для работы рекомендуется использовать браузер Vivaldi, Edge, Opera, Chrome - в общем все на базе Chromium."
                    },
                    {
                        view: "label",
                        id: "swaggerLink",
                        label: "<a href=\"scalar/v1\" target=\"_blank\" style=\"color: red\">Консоль запросов swagger.</a>."
                    },
                    {},
                ]
            }
        ]
    };
}

function init() {
    webix.ajax().get(ApiServerAdres("/configuration/about"))
        .then(function (data) {
            var _appVersion = data.text();
            $$("appVersion").setValue(_appVersion);
        });
}