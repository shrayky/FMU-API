import { SaveFormData } from "../../utils/net.js";
import { TableToolabr, Label, TextBox, PasswordBox, TextBoxFormated, CheckBox } from "../../utils/ui.js";
import { HostToPingForm } from "./hostToPingForm.js";
import { OrganisationForm } from "./organisationForm.js";

export function SettingsView(id) {
    $$("toolbarLabel").setValue("FMU-API: Настройка");

    const padding = {
        top: 5,
        bottom: 5,
        left: 20,
        right: 20
    };

    return {
        view: "form",
        id: "FmuApiSettingsForm",
        name: "fmuApiSettingsForm",
        url: "api->/configuration/parameters",
        save:
        {
            url: "api->/configuration/parameters",
            autoupdate: false
        },
        elements: [
            {
                cols: [
                    {
                        view: "button",
                        value: "Сохранить",
                        id: "saveBtn",
                        autowidth: "false",
                        width: 400,
                        click: SaveFormData
                    },

                    {}

                ]
            },

            {
                view: "scrollview",
                id: id,
                body: {
                    rows:
                    [
                        Label("LServerConfig", "Настройки сервера", { "fontSize": 22 }),

                        {
                            padding: padding,
                            rows: [
                                TextBox("text", "Название узла", "nodeName"),
                                {
                                    view: "subform",
                                    name: "serverConfig",
                                    elements: [
                                        {
                                            cols: [
                                                TextBoxFormated("number", "IP-порт API сервиса", "apiIpPort", "111"),
                                                TextBoxFormated("number", "IP-порт View сервиса", "viewIpPort", "111"),
                                            ]
                                        }
                                    ]
                                },
                            ]
                        },
            
                        Label("lOrganizations", "Организации"),
            
                        {
                            view: "subform",
                            name: "organisationConfig",
                            padding: padding,
                            elements: [
                                TableToolabr("PrintGroups"),
            
                                {
                                    view: "formtable",
                                    resizeColumn: true,
                                    resizeRow: true,
                                    select: true,
                                    minHeight: 250,
                                    id: "PrintGroups",
                                    name: "printGroups",
                                    columns: [
                                        { id: "id", header: "Код" },
                                        { id: "xapikey", header: "XAPIKEY", fillspace: true },
                                        { id: "inn", header: "ИНН", fillspace: true },
                                    ],
                                    on:
                                    {
                                        onAfterSelect: (selection, preserve) => {
                                            $$("delete_PrintGroups").enable();
                                        },
                                        onAfterDelete: (id) => {
                                            $$("delete_PrintGroups").disable();
            
                                            if ($$("OrganisationConfig").count() == 0)
                                                $$("deleteAll_PrintGroups").disable();
                                        },
                                        onBeforeAdd: (id, obj, idx) => {
                                            if (obj.xapikey == undefined) {
                                                OrganisationForm("Новая организация", "PrintGroups");
                                                return false;
                                            }
                                        },
                                        onItemDblClick: (id) => {
                                            OrganisationForm("Организация", "PrintGroups", id);
                                        }
                                    }
                                }
                            ]
                        },
            
                        Label("lHostsToPing", "Адреса сайтов для проверки доступности интернета (если проверка не нужна, список должен быть пустым)"),
            
                        {
                            padding: padding,
                            rows: [
                                TableToolabr("HostsToPing"),
            
                                {
                                    view: "formtable",
                                    id: "HostsToPing",
                                    name: "hostsToPing",
                                    resizeColumn: true,
                                    resizeRow: true,
                                    select: true,
                                    minHeight: 250,
                                    columns: [
                                        { id: "id", header: "Код", hidden: true },
                                        { id: "value", header: "Хост", fillspace: true },
                                    ],
                                    on:
                                    {
                                        onAfterSelect: _ => {
                                            $$("delete_HostsToPing").enable();
                                        },
                                        onAfterDelete: (id) => {
                                            $$("delete_HostsToPing").disable();
            
                                            if ($$("HostsToPing").count() == 0)
                                                $$("deleteAll_HostsToPing").disable();
                                        },
                                        onBeforeAdd: (id, obj, idx) => {
                                            if (obj.value == undefined) {
                                                HostToPingForm("Новый сайт", "HostsToPing");
                                                return false;
                                            }
                                        },
                                        onItemDblClick: (id) => {
                                            HostToPingForm("Cайт", "HostsToPing", id);
                                        }
                                    }
                                },
                            ]
                        },
            
                        Label("lDatabase", "База данных"),
            
                        {
                            view: "subform",
                            name: "database",
                            padding: padding,
                            elements: [
                                TextBox("text", "Адрес сервера CouchDb", "netAdres"),
                                {
                                    cols: [
                                        TextBox("text", "Пользователь", "userName"),
                                        PasswordBox("Пароль", "password")
                                    ]
                                },
                                TextBox("text", "База данных марок", "marksStateDbName"),
                                TextBox("text", "База документов кассы", "frontolDocumentsDbName"),
                                TextBox("text", "База марок алкоголя", "alcoStampsDbName"),
                            ]
            
                        },
            
                        Label("lMinimalPrices", "Минимальные цены продажи"),
            
                        {
                            view: "subform",
                            name: "minimalPrices",
                            padding: padding,
                            elements: [
                                TextBoxFormated("number", "Табак (в копейка 129р = 12900)", "tabaco", "1111")
                            ]
                        },
            
                        Label("lFrontolConnection", "Настройка подключения к БД Frontol"),

                        {
                            view: "subform",
                            name: "frontolConnectionSettings",
                            padding: padding,
                            elements: [
                                TextBox("text", "Путь к файлу main.gdb", "path"),
                                TextBox("text", "Пользователь Firebird", "frontolUserName", {"id": ""}),
                                TextBox("text", "Пароль Firebird", "frontolPassword"),
                            ]
                        },

                        Label("lFrontolAlcoUnit", "Настройка подключения к Frontol: AlcoUnit"),

                        {
                            view: "subform",
                            name: "frontolAlcoUnit",
                            padding: padding,
                            elements: [
                                TextBox("text", "Адрес сервиса Frontol AlcoUnit", "FrontolAuNetAdres"),
                                TextBox("text", "Пользователь", "frontolAuUserName", {"id": ""}),
                                TextBox("text", "Пароль", "frontolAuPassword"),
                            ]
                        },

                        Label("lTruesignService", "Сервис получения токена авторизации Четсного знака"),
            
                        {
                            view: "subform",
                            name: "trueSignTokenService",
                            padding: padding,
                            elements: [
                                TextBox("text", "Адрес сервиса", "connectionAddres")
                            ]
                        },
            
                        Label("lTimeoutConfig", "Настройка таймаутов"),
            
                        {
                            view: "subform",
                            name: "httpRequestTimeouts",
                            padding: padding,
                            elements: [
                                TextBoxFormated("number", "Загрузка списка cdn", "cdnRequestTimeout", "1111"),
                                TextBoxFormated("number", "Проверка марки в ЧЗ", "checkMarkRequestTimeout", "1111"),
                                TextBoxFormated("number", "Проверка доступа в интернет", "checkInternetConnectionTimeout", "1111"),
                            ]
                        },
            
            
                        Label("lLoggingLabel", "Настройка логгирования"),
                        {
                            view: "subform",
                            name: "logging",
                            padding: padding,
                            elements: [
                                CheckBox("Иcпользовать", "isEnabled"),
                                TextBoxFormated("number", "Сколько дней хранить файлы лога", "logDepth", "111"),
                                {
                                    view: "select",
                                    label: "Уровень логгирования",
                                    labelPosition: "top",
                                    id: "LogLevel",
                                    name: "logLevel",
                                    options: ["Verbose", "Debug", "Information", "Warning", "Error", "Fatal"]
                                },
            
                            ]
                        }


                    ]
                }
            }

        ]
    }
}
