import { Number, Label, padding, CheckBox, Text } from "../../../utils/ui.js";
import { httpAddressValidation } from "../../../utils/validators.js";

class CentralServerConnectionElement {
    constructor(id) {
        this.id = id;
        this.SETTINGS_ID = "serverSettings";
        this.FORM_NAME = "centralServerConnectionSettings";
        this.LABELS = {
            title: "Настройка подключения к сервису мониторинга",
            enabled: "Использовать",
            address: "Веб-адрес сервиса",
            token: "Токен",
            secret: "Секретный ключ",
            interval: "Интервал обмена (минут)"
        };
    }

    loadConfig(config) {

        if (config?.fmuApiCentralServer) {
            this.enabled = config.fmuApiCentralServer.enabled;
            this.address = config.fmuApiCentralServer.address;
            this.token = config.fmuApiCentralServer.token;
            this.interval = config.fmuApiCentralServer.exchangeRequestInterval;
        }
        return this;
    }

    render() {
        const SETTINGS_ID = this.SETTINGS_ID;

        var elements = [];

        elements.push(
            Label("lCentralServerConnection", this.LABELS.title)
        );

        elements.push(
            {
                padding: padding,
                rows: [
                    CheckBox("Иcпользовать", "fmuApiCentralServer.enabled", {
                        value: this.enabled,
                        on: {
                            onChange: function(enabled) {
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
                        id: this.SETTINGS_ID,
                        disabled: !this.enabled,
                        rows: [
                            Text(this.LABELS.address,
                                 "fmuApiCentralServer.address",
                                 this.address,
                                 httpAddressValidation),

                            Text(this.LABELS.token,
                                 "fmuApiCentralServer.token",
                                 this.token),

                            Text(this.LABELS.secret,
                                 "fmuApiCentralServer.secret",
                                 this.secret),

                            Number(this.LABELS.interval,
                                 "fmuApiCentralServer.exchangeRequestInterval",
                                this.interval)
                        ],
                    }
                ]
            }
        );

        return { id: this.id, rows: elements };
    }
}

export default function (id, config) {
    return new CentralServerConnectionElement(id)
        .loadConfig(config)
        .render();
}