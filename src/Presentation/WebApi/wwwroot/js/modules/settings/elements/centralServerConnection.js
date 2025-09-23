import { TextBox, Label, padding, CheckBox } from "../../../utils/ui.js";
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
            interval: "Интервал обмена (секунд)"
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
                        disabled: true,
                        rows: [
                            TextBox("text", this.LABELS.address, "fmuApiCentralServer.address", httpAddressValidation),
                            TextBox("text", this.LABELS.token, "fmuApiCentralServer.token"),
                            TextBox("text", this.LABELS.secret, "fmuApiCentralServer.secret"),
                            TextBox("number", this.LABELS.interval, "fmuApiCentralServer.exchangeRequestInterval")
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