/**
 * Форма редактирования организации (группы печати).
 */
import { Text, Number, PasswordBox, CheckBox } from "../../../utils/ui.js";

export function showOrganizationForm({ labels, formName, tableId, id, onSave }) {
    const windowInnerWidth = window.innerWidth;

    if ($$(formName))
        $$(formName).destructor();

    webix.ui({
        view: "window",
        id: formName,
        position: "center",
        modal: true,
        move: false,
        resize: false,
        width: Math.min(windowInnerWidth * 0.8, 900),
        head: {
            view: "toolbar",
            elements: [
                { view: "label", label: id == undefined ? labels.newOrg : labels.editOrg },
                {
                    view: "icon",
                    icon: "wxi-close",
                    click: () => $$(formName).close()
                }
            ]
        },
        body: createFormBody(labels, tableId, id, formName, onSave)
    }).show();

    initFormValues(tableId, id);
}

function createFormBody(labels, tableId, id, formName, onSave) {
    return {
        padding: 10,
        rows: [
            Number(labels.code, "OrganizationId", "1111"),
            {
                cols: [
                    Text(labels.name, "OrganizationName"),
                    Text(labels.inn, "OrganizationInn"),
                ]
            },
            {
                view: "tabview",
                height: 300,
                cells: [
                    {
                        header: "ТСПИоТ",
                        body: {
                            padding: 10,
                            rows: [
                                {
                                    cols: [
                                        Text(labels.tsPiotHost, "TsPiotHost", "", { placeholder: "localhost" }),
                                        Text(labels.tsPiotPort, "TsPiotPort", "", { placeholder: "51401" }),
                                    ],
                                },
                                {
                                    view: "label",
                                    label: "Адрес получения информации о модуле ТСПИоТ",
                                },
                                {
                                    cols: [
                                        Text(labels.tsPiotinformationEndpoint, "TsPiotinformationEndpoint", "", { placeholder: "/api/v1/info" }),
                                        Text(labels.tsPiotInformationPort, "TsPiotInformationPort", "", { placeholder: "51077" }),
                                    ]
                                },
                                {},
                                {
                                    view: "template",
                                    template: "Для актуальных версий фронтола (больше 6.28.0) адрес подключения настраивается в ККМ и передается в запросе проверки марки.",
                                    css: {
                                        "white-space": "normal",
                                        "word-wrap": "break-word",
                                        "line-height": "1.4",
                                        "padding": "5px 0"
                                    },
                                    autoheight: true
                                },
                            ]
                        }
                    },
                    {
                        header: "Локальный модуль",
                        body: {
                            padding: 10,
                            rows: [
                                CheckBox(labels.enable, "LocalModuleEnable"),
                                Text(labels.connectionAddress, "LocalModuleConnectionAddress", "", {
                                    placeholder: "http://hostname:5995"
                                }),
                                {
                                    cols: [
                                        Text(labels.userName, "LocalModuleUserName"),
                                        PasswordBox(labels.password, "LocalModulePassword"),
                                    ]
                                },
                                Text(labels.eniseyConnectionAddress, "EniseyConnectionAddress", "", {
                                    placeholder: "http://hostname:5984"
                                })
                            ]
                        }
                    },
                    {
                        header: "Разрешительный режим (до 01.10.2026)",
                        body: {
                            padding: 10,
                            rows: [
                                Text(labels.xapikey, "XAPIKEY"),
                            ]
                        }
                    },
                    {
                        header: "True api интеграция",
                        body: {
                            padding: 10,
                            rows: [
                                CheckBox(labels.enable, "TrueApiIntegrationEnable"),
                                {
                                    view: "richselect",
                                    id: "TrueApiIntegrationDigitalSignature",
                                    label: labels.DigitalSignature,
                                    labelPosition: "top",
                                    placeholder: "Выберите сертификат",
                                    options: [],
                                    on: {
                                        onBeforeRender: async function () {
                                            const control = this;
                                            if (control.config._loaded)
                                                return;

                                            try {
                                                const response = await fetch("/api/digitalsignature");
                                                if (!response.ok) {
                                                    webix.message({ text: "Не удалось загрузить сертификаты", type: "error" });
                                                    return;
                                                }

                                                const certificates = await response.json();
                                                const options = certificates.map(c => ({
                                                    id: c.number,
                                                    value: `${c.presentation}`
                                                }));

                                                const popup = control.getPopup();
                                                popup.getList().clearAll();
                                                popup.getList().parse(options);
                                                control.config._loaded = true;
                                            } catch (e) {
                                                webix.message({ text: e?.message || "Ошибка загрузки сертификатов", type: "error" });
                                            }
                                        }
                                    }
                                },
                                Text(labels.signPassword, "TrueApiIntegrationPassword"),
                                {
                                    cols: [
                                        {
                                            view: "button",
                                            value: labels.LoadToken,
                                            id: "loadTrueApiToken",
                                            width: 200,
                                            click: async () => {
                                                const inn = $$("OrganizationInn").getValue();
                                                if (!inn || !String(inn).trim()) {
                                                    webix.message({ text: "Укажите ИНН организации", type: "error" });
                                                    return;
                                                }
                                                try {
                                                    const response = await fetch(`/api/ts/token/inn?inn=${encodeURIComponent(inn)}`);
                                                    if (response.status === 404) {
                                                        webix.message({
                                                            text: "Токен ещё не готов. Он получается через 2 минуты после старта службы, а потом обновляется каждые 10 минут.",
                                                            type: "info"
                                                        });
                                                        return;
                                                    }
                                                    if (!response.ok) {
                                                        webix.message({ text: "Ошибка при получении токена", type: "error" });
                                                        return;
                                                    }
                                                    const data = await response.json();
                                                    const token = data.token ?? data.Token ?? "";
                                                    if (!token) {
                                                        webix.message({ text: "Токен не получен", type: "error" });
                                                        return;
                                                    }
                                                    await navigator.clipboard.writeText(token);
                                                    webix.message("Токен получен и скопирован в буфер обмена");
                                                } catch (e) {
                                                    webix.message({ text: e?.message || "Ошибка при получении токена", type: "error" });
                                                }
                                            }
                                        },
                                        {}
                                    ]
                                },
                                {
                                    view: "template",
                                    template: "Для работы необходимо что бы на одном ПК с fmu-api был установлен КриптоПро, а так же у пользователя от которого запущена fmu-api был установлен сертификат ЭЦП.",
                                    css: {
                                        "white-space": "normal",
                                        "word-wrap": "break-word",
                                        "line-height": "1.4",
                                        "padding": "5px 0"
                                    },
                                    autoheight: true
                                }
                            ]
                        }
                    }
                ]
            },
            {
                padding: { top: 10 },
                cols: [
                    {
                        view: "button",
                        value: labels.add,
                        width: 200,
                        click: () => onSave(tableId, id)
                    },
                    {
                        view: "button",
                        value: labels.close,
                        width: 200,
                        click: () => $$(formName).close()
                    },
                    {}
                ]
            }
        ]
    };
}

function initFormValues(tableId, id) {
    const table = $$(tableId);

    if (id == undefined) {
        const lastId = table.getLastId();
        $$("OrganizationId").setValue(lastId == undefined ? 1 : +lastId + 1);
        $$("TsPiotInformationPort").setValue(51077);
        return;
    }

    const item = table.getItem(id);
    $$("OrganizationId").setValue(item.id);
    $$("OrganizationId").disable();
    $$("OrganizationInn").setValue(item.inn);
    $$("OrganizationName").setValue(item.name);
    $$("TsPiotHost").setValue(item.tsPiot?.host ?? "");
    $$("TsPiotPort").setValue(item.tsPiot?.port ?? "");
    $$("TsPiotInformationPort").setValue(item.tsPiot?.informationPort ?? 51077);
    $$("TsPiotinformationEndpoint").setValue(item.tsPiot?.informationEndpoint ?? "");
    $$("LocalModuleEnable").setValue(!!item.localModuleConnection?.enable);
    $$("LocalModuleConnectionAddress").setValue(item.localModuleConnection?.connectionAddress ?? "");
    $$("LocalModuleUserName").setValue(item.localModuleConnection?.userName ?? "");
    $$("LocalModulePassword").setValue(item.localModuleConnection?.password ?? "");
    $$("EniseyConnectionAddress").setValue(item.localModuleConnection?.eniseyConnectionAddress ?? "");
    $$("XAPIKEY").setValue(item.xapikey ?? "");

    const trueApi = item.trueApiIntegrationSettings ?? {};
    $$("TrueApiIntegrationEnable").setValue(!!trueApi.enable);
    $$("TrueApiIntegrationPassword").setValue(trueApi.password ?? "");
    $$("TrueApiIntegrationDigitalSignature").setValue(trueApi.digitalSignature ?? "");
}

/**
 * Собирает данные организации из формы.
 */
export function collectOrganizationFormData(table, id) {
    const informationPortRaw = $$("TsPiotInformationPort").getValue();
    const informationPort = parseInt(informationPortRaw, 10);
    const existingGroups = id != undefined
        ? (table.getItem(id)?.trueApiIntegrationSettings?.productGroups ?? [])
        : [];

    return {
        id: $$("OrganizationId").getValue(),
        xapikey: $$("XAPIKEY").getValue(),
        tsPiot: {
            host: $$("TsPiotHost").getValue(),
            port: $$("TsPiotPort").getValue(),
            informationPort: isNaN(informationPort) ? 51077 : informationPort,
            informationEndpoint: $$("TsPiotinformationEndpoint").getValue(),
        },
        inn: String($$("OrganizationInn").getValue() || "").trim(),
        name: $$("OrganizationName").getValue(),
        localModuleConnection: {
            enable: !!$$("LocalModuleEnable").getValue(),
            connectionAddress: $$("LocalModuleConnectionAddress").getValue(),
            userName: $$("LocalModuleUserName").getValue(),
            password: $$("LocalModulePassword").getValue(),
            eniseyConnectionAddress: $$("EniseyConnectionAddress").getValue()
        },
        trueApiIntegrationSettings: {
            enable: !!$$("TrueApiIntegrationEnable").getValue(),
            password: $$("TrueApiIntegrationPassword").getValue(),
            digitalSignature: $$("TrueApiIntegrationDigitalSignature").getValue(),
            productGroups: existingGroups
        }
    };
}
