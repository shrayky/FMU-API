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
        id: id,
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
                borderless: false,
                id: `${id}_scroll`,
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
                                                TextBox("number", "IP-порт API сервиса", "apiIpPort", {"format": "111"}),
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
            
                        Label("lHostsToPing", "Адреса сайтов для проверки доступности интернета (если проверка не нужна, то список должен быть пустым)"),
            
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

                                {
                                    view: "button",
                                    id: "fauxton_open",
                                    value: "Открыть Fauxton",
                                    inputWidth: 180,
                                    inputHeight: 40,
                                    click: _ => {
                                        let adres = $$("netAdres").getValue();

                                        if (adres != "")
                                            window.open(`${adres}/_utils`, "_blank").focus();
                                    }
                                },
                            ]
            
                        },
            
                        Label("lMinimalPrices", "Минимальные цены продажи"),
            
                        {
                            view: "subform",
                            name: "minimalPrices",
                            padding: padding,
                            elements: [
                                TextBox("number", "Табак (в копейках 129р = 12900)", "tabaco", {"format": "1111"})
                            ]
                        },
            
                        Label("lFrontolConnection", "Подключение к БД Frontol"),

                        {
                            view: "subform",
                            name: "frontolConnectionSettings",
                            padding: padding,
                            elements: [
                                TextBox("text", "Путь к файлу main.gdb", "path"),
                                TextBox("text", "Пользователь Firebird", "userName", {"id": "frontolUserName"}),
                                TextBox("text", "Пароль Firebird", "password", {"id": "frontolPassword"}),
                            ]
                        },

                        Label("lFrontolAlcoUnit", "Frontol: AlcoUnit"),

                        {
                            view: "subform",
                            name: "frontolAlcoUnit",
                            padding: padding,
                            elements: [
                                TextBox("text", "Адрес сервиса Frontol AlcoUnit", "NetAdres", {"id": "auNetAdres"}),
                                TextBox("text", "Пользователь", "UserName", {"id": "auUserName"}),
                                TextBox("text", "Пароль", "Password", {"id": "auPassword"}),
                            ]
                        },

                        Label("lTruesignService", "Сервис получения токена авторизации Честного знака"),
            
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
                                TextBox("number", "Загрузка списка cdn", "cdnRequestTimeout", {"format": "1111"}),
                                TextBox("number", "Проверка марки в ЧЗ", "checkMarkRequestTimeout", {"format": "1111"}),
                                TextBox("number", "Проверка доступа в интернет", "checkInternetConnectionTimeout", {"format": "1111"}),
                            ]
                        },

                        Label("lSaleseControlParametrs", "Настройка контроля при продажи товаров"),

                        {
                            view: "subform",
                            name: "saleControlConfig",
                            padding: padding,
                            elements: [
                                CheckBox("Блокировать продажу возвращенных товаров", "banSalesReturnedWares"),
                                CheckBox("Проверять товары из чеков возврата", "checkReceiptReturn"),
                                TextBox("text", "Коды групп товаров игнорирующик проверку стутсов кода маркировки в Честном Знаке", "ignoreVerificationErrorForTrueApiGroups", {"id": "tbIgnoreVerificationErrorForTrueApiGroups"}),
                            ]
                        },
            
            
                        Label("lLoggingLabel", "Настройка логирования"),
                        {
                            view: "subform",
                            name: "logging",
                            padding: padding,
                            elements: [
                                CheckBox("Иcпользовать", "isEnabled"),
                                TextBox("number", "Сколько дней хранить файлы лога", "logDepth", {"format": "1111"}),
                                {
                                    view: "select",
                                    label: "Уровень логирования",
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
