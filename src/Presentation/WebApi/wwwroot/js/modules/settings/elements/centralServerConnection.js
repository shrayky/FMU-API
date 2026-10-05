import { Number, padding, CheckBox, Text } from "../../../utils/ui.js";
import { httpAddressListValidation } from "../../../utils/validators.js";
import updateInstallSchedule from "./updateInstallSchedule.js";

const SCHEDULER_PREFIX = "scheduler";
const SCHEDULER_SETTINGS_ID = `${SCHEDULER_PREFIX}Settings`;

class CentralServerConnectionElement {
    constructor(id) {
        this.id = id;
        this.SETTINGS_ID = "serverSettings";
        this.LABELS = {
            enabled: "Использовать",
            address: "Веб-адрес сервиса",
            token: "Токен",
            secret: "Секретный ключ",
            interval: "Интервал обмена (минут)",
            downloadNewVersion: "Загружать и устанавливать новую версию",
            doExchangeWithServer: "Выполнить обмен",
            addressTip: "⚠️можно указать несколько адресов через точку с запятой",
        };
    }

    loadConfig(config) {
        if (config?.fmuApiCentralServer) {
            const settings = config.fmuApiCentralServer;

            this.enabled = settings.enabled;
            this.address = settings.address;
            this.token = settings.token;
            this.secret = settings.secret;
            this.interval = settings.exchangeRequestInterval;
            this.downloadNewVersion = settings.downloadNewVersion;
            this.schedulerUpdateInstall = settings.schedulerUpdateInstall ?? [];
        }

        return this;
    }

    render() {
        const SETTINGS_ID = this.SETTINGS_ID;

        var elements = [];

        elements.push(
            {
                padding: padding,
                rows: [
                    CheckBox(this.LABELS.enabled, "fmuApiCentralServer.enabled", {
                        value: this.enabled,
                        on: {
                            onChange: function (enabled) {
                                if (enabled) {
                                    $$(SETTINGS_ID).enable();
                                }
                                else {
                                    $$(SETTINGS_ID).disable();
                                }
                            }
                        }
                    }),

                    {
                        cols: [
                                {
                                view: "button",
                                value: this.LABELS.doExchangeWithServer,
                                css: "webix_primary",
                                width: 300,
                                click: function () {
                                    webix.ajax()
                                        .get("/api/centralServer/centralServerExchange")
                                        .then(function (response) {
                                            webix.message({
                                                text: "Обмен с центральным сервером выполнен успешно",
                                                type: "success"
                                            });
                                        })
                                        .fail(function (xhr) {
                                            webix.message({
                                                text: "Ошибка обмена с центральным сервером: " + xhr.responseText,
                                                type: "error"
                                            });
                                        });
                                }
                            },

                            {}
                        ]
                    },
                    
                    {
                        id: this.SETTINGS_ID,
                        disabled: !this.enabled,
                        rows: [
                            this._labelWithTip("lCentralServerAddress", this.LABELS.address, this.LABELS.addressTip),

                            Text("",
                                "fmuApiCentralServer.address",
                                this.address,
                                httpAddressListValidation),

                            Text(this.LABELS.token,
                                "fmuApiCentralServer.token",
                                this.token),

                            Text(this.LABELS.secret,
                                "fmuApiCentralServer.secret",
                                this.secret),

                            Number(this.LABELS.interval,
                                "fmuApiCentralServer.exchangeRequestInterval",
                                this.interval),

                            CheckBox(this.LABELS.downloadNewVersion, "fmuApiCentralServer.downloadNewVersion", {
                                value: this.downloadNewVersion,
                                on: {
                                    onChange: (enabled) => {
                                        if (enabled) {
                                            $$(SCHEDULER_SETTINGS_ID).enable();
                                        }
                                        else {
                                            $$(SCHEDULER_SETTINGS_ID).disable();
                                        }
                                    }
                                }
                            }),

                            updateInstallSchedule({
                                prefix: SCHEDULER_PREFIX,
                                name: "fmuApiCentralServer.schedulerUpdateInstall",
                                data: this.schedulerUpdateInstall,
                                disabled: !this.downloadNewVersion,
                            }),

                        ],
                    }
                ]
            }
        );

        return { id: this.id, rows: elements };
    }

    _labelWithTip(id, title, tip) {
        const safeTitle = webix.template.escape(title);
        const safeTip = webix.template.escape(tip);

        return {
            cols: [
                {
                    view: "label",
                    id: id,
                    label: title,
                    width: 0,
                    autowidth: true,
                },
                {
                    view: "template",
                    id: `${id}Tip`,
                    borderless: true,
                    css: "webix_el_label",
                    template: `<div style="display:flex;align-items:center;height:100%"><span style="font-style:italic;font-size:smaller">${safeTip}</span></div>`
                }
            ]
        };
    }
}

export default function (id, config) {
    return new CentralServerConnectionElement(id)
        .loadConfig(config)
        .render();
}
