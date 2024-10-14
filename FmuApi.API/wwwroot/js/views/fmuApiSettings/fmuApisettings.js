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

            {
                view: "scrollview",
                borderless: true,
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

                            Label("lLoggingLabel", "Настройка логирования"),
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
                            },

                            Label("lAutoupdateLabel", "Настройка автообновления"),

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

                                                        console.log(values);
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
                    ]
                }
            }

        ]
    }
}
