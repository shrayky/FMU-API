import { ApiServerAddress } from '../../utils/net.js';

export default function informationView(id) {
    $$("toolbarLabel").setValue("FMU-API: Информация о системе");

    let appVersion = "Версия загружается...";

    return {
        id,
        rows: [
            {
                view: "form",
                on: {
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

                    {
                        view: "label",
                        id: "supportLinks",
                        label: "<strong>Поддержка проекта:</strong>"
                    },

                    {
                        view: "label",
                        id: "telegramGroup",
                        label: "• &#128172 <a href=\"https://t.me/frntlsc\" target=\"_blank\" style=\"color: #0088cc\">Telegram канал поддержки</a>"
                    },

                    {
                        view: "label",
                        id: "gitHubPage",
                        label: "• &#128736; <a href=\"https://github.com/shrayky/FMU-API\" target=\"_blank\" style=\"color: #0088cc\">GitHub репозиторий</a>"
                    },

                    {
                        view: "label",
                        id: "moneySupport",
                        label: "• &#128176; <a href=\"https://pay.cloudtips.ru/p/1fb36b3c\" target=\"_blank\" style=\"color: #0088cc\">Поддержать проект</a>"
                    },

                    {},
                ]
            }
        ]
    };
}

function init() {
    webix.ajax().get(ApiServerAddress("/configuration/about"))
        .then(function (data) {
            var _appVersion = data.text();
            $$("appVersion").setValue(_appVersion);
        });
}