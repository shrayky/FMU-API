import { SaveFormData } from "../../utils/net.js";
import { TableToolabr, Label, TextBox, PasswordBox, TextBoxFormated, CheckBox } from "../../utils/ui.js";
import { HostToPingForm } from "./hostToPingForm.js";
import { OrganisationForm } from "./organisationForm.js";

const padding = {
    top: 5,
    bottom: 5,
    left: 20,
    right: 20
};

const optionsArray = (nums) => {
    let vl;
    const units = [];

    for (let i = 1; i < nums + 1; i += 1) {
        if (i >= 0 && i <= 10)
            vl = "0" + (i - 1);
        else
            vl = i - 1;

        units.push({ id: i, value: vl.toString() });
    }
    return units;
}
export function SettingsView(id) {
    $$("toolbarLabel").setValue("FMU-API: Настройка");

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
        on: {
            "onAfterLoad": () => {
                let values = $$("AutoUpdate").getValue();
                $$("AutoUpdate").setValues({ "fromHourW": values.fromHour + 1, "untilHourW": values.untilHour + 1 }, true)
            }
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

            scrollViewConfuguration(id)

        ]
    }
}

function scrollViewConfuguration(id) {
    return {
        view: "scrollview",
        borderless: true,
        id: `${id}_scroll`,
        body: {
            rows:
                [
                    serverConfigurationDta("serverConfigData"),

                    loggingConfigurationData("loggingConfigData"),
        
                    autoUpdateData("autoUpdateData"),
        
                    organisations("organisationsData"),

                    checkInternetConnectionHosts("checkInternetConnectionHosts"),
        
                    databaseConfig("couchDbData"),
        
                    frontolConnection("frontolDbConnection"),
        
                    frontolMarkUnitConnection("frontolMarkUnit"),
        
                    serviceLoaderTokenForTeuApi("tokenServiceData"),
        
                    timeoutConfig("timeoutConfig"),
        
                    saleControlData("salesControl"),
        
                    minimalSalePrices("minimalPrices"),
                ]
        }
    }
}

function serverConfigurationDta(id) {
    var elements = [];

    elements.push(
        Label("LServerConfig", "Настройки сервера")
    );

    elements.push(
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
                                TextBox("number", "IP-порт API сервиса", "apiIpPort", { "format": "111" }),
                            ]
                        }
                    ]
                },
            ]
        }
    );

    return { id: id, rows: elements }
}

function loggingConfigurationData(id) {
    var elements = [];

    elements.push(
        Label("lLoggingLabel", "Настройка логирования")
    );

    elements.push(
        {
            view: "subform",
            name: "logging",
            padding: padding,
            elements: [
                CheckBox("Иcпользовать", "isEnabled"),
                TextBox("number", "Сколько дней хранить файлы лога", "logDepth", { "format": "1111" }),
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
    );

    return { id: id, rows: elements }
}

function autoUpdateData(id) {
    var elements = [];

    elements.push(
        Label("lAutoupdateLabel", "Настройка автообновления"),
    );

    elements.push(
        {
            view: "subform",
            name: "autoUpdate",
            id: "AutoUpdate",
            padding: padding,
            elements: [
                CheckBox("Использовать", "enabled", { "id": "autoUpdate_enabled" }),

                TextBox("text", "Каталог с файлами обновлений", "updateFilesCatalog"),

                Label("AutoupdateTimeLabel", "Часы для автообновления"),

                {
                    padding: { left: 20, },
                    cols: [
                        {
                            view: "combo",
                            id: "fromHourW",
                            name: "fromHourW",
                            options: optionsArray(24),
                            maxWidth: 60,
                            format: "1111",
                            newValues: false,
                            on: {
                                "onChange": (newValue) => {
                                    $$("AutoUpdate").setValues({ "fromHour": newValue - 1 }, true)
                                },
                                "onViewShow": () => {
                                    let values = $$("AutoUpdate").getValue();
                                }

                            }
                        },

                        Label("AutoupdateTimeSeparator", "-", { "maxWidth": 5 }),

                        {
                            view: "combo",
                            id: "untilHourW",
                            name: "untilHourW",
                            options: optionsArray(24),
                            maxWidth: 60,
                            format: "1111",
                            newValues: false,
                            on: {
                                "onChange": (newValue) => {
                                    $$("AutoUpdate").setValues({ "untilHour": newValue - 1 }, true)
                                }
                            }
                        },

                        {}
                    ]
                },

            ],
        }
    );

    return { id: id, rows: elements }
}

function checkInternetConnectionHosts(id) {
    var elements = [];

    elements.push(
        Label("lHostsToPing", "Адреса сайтов для проверки доступности интернета (если проверка не нужна, то список должен быть пустым)"),
    );

    elements.push(
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
        }
    );

    return { id: id, rows: elements }
}

function organisations(id) {
    var elements = [];

    elements.push(
        Label("lOrganizations", "Организации")
    );

    elements.push(
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
        });

    return { id: id, rows: elements }
}

function databaseConfig(id) {
    var elements = [];

    elements.push(
        Label("lDatabase", "База данных")
    );

    elements.push(
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
        }
    );

    elements.push(
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
    );

    return { id: id, rows: elements }
}

function minimalSalePrices(id) {
    var elements = [];

    elements.push(
        Label("lMinimalPrices", "Минимальные цены продажи"),
    );

    elements.push(
        {
            view: "subform",
            name: "minimalPrices",
            padding: padding,
            elements: [
                TextBox("number", "Табак (в копейках 129р = 12900)", "tabaco", { "format": "1111" })
            ]
        },
    );

    return { id: id, rows: elements }
}

function frontolConnection(id) {
    var elements = [];

    elements.push(
        Label("lFrontolConnection", "Подключение к БД Frontol")
    );

    elements.push(
        {
            view: "subform",
            name: "frontolConnectionSettings",
            padding: padding,
            elements: [
                TextBox("text", "Путь к файлу main.gdb", "path"),
                TextBox("text", "Пользователь Firebird", "userName", { "id": "frontolUserName" }),
                TextBox("text", "Пароль Firebird", "password", { "id": "frontolPassword" }),
            ]
        }
    );

    return { id: id, rows: elements }
}

function frontolMarkUnitConnection(id) {
    var elements = [];

    elements.push(
        Label("lFrontolAlcoUnit", "Frontol: AlcoUnit")
    );

    elements.push(
        {
            view: "subform",
            name: "frontolAlcoUnit",
            padding: padding,
            elements: [
                TextBox("text", "Адрес сервиса Frontol AlcoUnit", "NetAdres", { "id": "auNetAdres" }),
                TextBox("text", "Пользователь", "UserName", { "id": "auUserName" }),
                TextBox("text", "Пароль", "Password", { "id": "auPassword" }),
            ]
        });

    return { id: id, rows: elements }

}

function serviceLoaderTokenForTeuApi(id) {
    var elements = [];

    elements.push(
        Label("lTruesignService", "Сервис получения токена авторизации Честного знака"),
    );

    elements.push(
        {
            view: "subform",
            name: "trueSignTokenService",
            padding: padding,
            elements: [
                TextBox("text", "Адрес сервиса", "connectionAddres")
            ]
        });

    return { id: id, rows: elements }
}

function timeoutConfig(id) {
    var elements = [];

    elements.push(
        Label("lTimeoutConfig", "Настройка таймаутов")
    );

    elements.push(
        {
            view: "subform",
            name: "httpRequestTimeouts",
            padding: padding,
            elements: [
                TextBox("number", "Загрузка списка cdn", "cdnRequestTimeout", { "format": "1111" }),
                TextBox("number", "Проверка марки в ЧЗ", "checkMarkRequestTimeout", { "format": "1111" }),
                TextBox("number", "Проверка доступа в интернет", "checkInternetConnectionTimeout", { "format": "1111" }),
            ]
        }
    );

    return { id: id, rows: elements }
}

function saleControlData(id) {
    var elements = [];

    elements.push(
        Label("lSaleseControlParametrs", "Настройка контроля при продажи товаров")
    );

    elements.push(
        {
            view: "subform",
            name: "saleControlConfig",
            padding: padding,
            elements: [
                CheckBox("Блокировать продажу возвращенных товаров", "banSalesReturnedWares"),
                TextBox("text", "Коды групп товаров игнорирующик проверку статусов кода маркировки в Честном Знаке", "ignoreVerificationErrorForTrueApiGroups", { "id": "tbIgnoreVerificationErrorForTrueApiGroups" }),
                CheckBox("Проверять владельца марки", "сheckIsOwnerField"),

                Label("scForFrontolMoreThen21", "Настройки для тарифного фронтола (6.21.0 и выше):"),

                {
                    padding: { left: 20, },
                    rows: [
                        CheckBox("Проверять товары из чеков возврата", "checkReceiptReturn"),
                        CheckBox("Генерирвать пустой ответ от честного знака при недоступности cdn", "sendEmptyTrueApiAnswerWhenTimeoutError"),
                    ]
                }

            ]
        }
    );

    return { id: id, rows: elements }
}
