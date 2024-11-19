import { InitProxy } from '../../js/utils/proxy.js';
import { SettingsView } from "./fmuApiSettings/fmuApisettings.js";
import { informationView } from './information/fmuInformation.js';
import { barcodeScaner } from './Scaner/barcodeScaner.js';
import { cdnView } from './cdnList/cdnInforamtion.js';
import { logsView } from './logs/logsViewer.js';

let currentPage = "";

const windowInnerHeight = window.innerHeight;
const bodyId = "body";

InitProxy();

webix.ready(function () {
    webix.ui({
        container: "app",
        type:"space",
        id: "root",
        responsive: true,
        rows: [
            {
                view: "toolbar",
                padding: 5,
                height: 60,
                elements:
                    [
                        {
                            view: "label",
                            id: "toolbarLabel",
                            label: "FMU-API"
                        }
                    ]
            },
            {
                cols:
                    [
                        {
                            view: "sidebar",
                            id: "sidebar",
                            width: 200,
                            height: windowInnerHeight - 80,
                            collapsed: false,
                            data:
                                [
                                    {
                                        id: "config",
                                        value: "Настройка",
                                        data: [
                                            {
                                                id: "serverConfigData",
                                                value: "Сервера"
                                            },
                                            
                                            {
                                                id: "loggingConfigData",
                                                value: "Логрование"
                                            },
                                            
                                            {
                                                id: "autoUpdateData",
                                                value: "Автоматическое обновление"
                                            },

                                            {
                                                id: "organisationsData",
                                                value: "Организации"
                                            },
                                            
                                            {
                                                id: "checkInternetConnectionHosts",
                                                value: "Проверка интернета"
                                            },
                                            
                                            {
                                                id: "couchDbData",
                                                value: "База данных"
                                            },
                                            
                                            {
                                                id: "frontolDbConnection",
                                                value: "Frontol"
                                            },

                                            {
                                                id: "frontolMarkUnit",
                                                value: "Frontol Markunit"
                                            },

                                            {
                                                id: "tokenServiceData",
                                                value: "Сервис получения токена ЧЗ"
                                            },

                                            {
                                                id: "timeoutConfig",
                                                value: "Таймауты"
                                            },

                                            {
                                                id: "salesControl",
                                                value: "Контроль продаж"
                                            },

                                            {
                                                id: "minimalPrices",
                                                value: "Минимальные цены"
                                            },
                                        ]
                                    },

                                    {
                                        id: "cdnListInfo",
                                        value: "Список CDN"
                                    },

                                    {
                                        id: "logsView",
                                        value: "Логи"
                                    },

                                    {
                                        id: "information",
                                        value: "Информация"
                                    },

                                    //{
                                    //    id: "scaner",
                                    //    value: "Сканер"
                                    //}
                                ],
                            on:
                            {
                                onAfterSelect: function (id) {
                                    if (id == currentPage)
                                        return;

                                    switch (id) {
                                        case "config":
                                            webix.ui(SettingsView(bodyId), $$(bodyId), $$(currentPage));
                                            currentPage = "config";
                                            break;
                                        case "information":
                                            webix.ui(informationView(bodyId), $$(bodyId), $$(currentPage));
                                            currentPage = "information";
                                            $$('sidebar').close("config");
                                            break;
                                        case "cdnListInfo":
                                            webix.ui(cdnView(bodyId), $$(bodyId), $$(currentPage));
                                            currentPage = "cdnListInfo";
                                            $$('sidebar').close("config");
                                            break;
                                        case "logsView":
                                            webix.ui(logsView(bodyId), $$(bodyId), $$(currentPage));
                                            currentPage = "logsView";
                                            $$('sidebar').close("config");
                                            break;
                                        case "scaner":
                                            webix.ui(barcodeScaner(bodyId), $$(bodyId), $$(currentPage));
                                            currentPage = "scaner";
                                            $$('sidebar').close("config");
                                            break;

                                        default:
                                            let elem = $$(id);

                                            if (elem == undefined)
                                                return;
                                            
                                            $$(`${bodyId}_scroll`).showView(id);
                                    }
                                },
                                onAfterOpen: function (id) {
                                    if (id == currentPage)
                                        return;

                                    switch (id) {
                                        case "config":
                                            webix.ui(SettingsView(bodyId), $$(bodyId), $$(currentPage));
                                            currentPage = "config";
                                            $$('sidebar').unselect();
                                            break;
                                        case "information":
                                            webix.ui(informationView(bodyId), $$(bodyId), $$(currentPage));
                                            currentPage = "information";
                                            $$('sidebar').close("config");
                                            break;
                                        case "cdnListInfo":
                                            webix.ui(cdnView(bodyId), $$(bodyId), $$(currentPage));
                                            currentPage = "cdnListInfo";
                                            $$('sidebar').close("config");
                                            break;
                                        case "logsView":
                                            webix.ui(cdnView(bodyId), $$(bodyId), $$(currentPage));
                                            currentPage = "logsView";
                                            $$('sidebar').close("config");
                                            break;
                                        case "scaner":
                                            webix.ui(barcodeScaner(bodyId), $$(bodyId), $$(currentPage));
                                            currentPage = "scaner";
                                            $$('sidebar').close("config");
                                            break;

                                        default:
                                            break;
                                    }
                                }
                            }
                        },
                        {
                            id: bodyId
                        }
                    ]
            }

        ]
    });

    $$('sidebar').open("config");

});

webix.event(window, "resize", function (e) {
    $$("root").resize();
})

webix.protoUI({
    name: "subform",
    defaults: {
        borderless: true,
        paddingX: 0,
        paddingY: 0
    },
    getValue: function () {
        return this.getValues();
    },
    setValue: function (values) {
        this.setValues(values);
    },
}, webix.ui.form);

webix.protoUI({
    name: "formtable",
    setValue: function (value) {
        this.clearAll();
        this.parse(value)
    },
    getValue: function () {
        return this.serialize();
    }
}, webix.ui.datatable);

webix.protoUI({
    name: "formlists",
    setValue: function (value) {
        this.clearAll();
        this.parse(value)
    },
    getValue: function () {
        return this.serialize();
    }
}, webix.ui.list);