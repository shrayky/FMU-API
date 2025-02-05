import { Label, TextBox, Number, padding, CheckBox } from "../../../utils/ui.js";

class TimeoutsConfigurationElement {
    constructor(id) {
        this.id = id;
        this.SETTINGS_ID = "timeoutsSettings";
        this.LABELS = {
            title: "Таймауты",
            cdnLoadTimeout: "Загрузка списка cdn, сек",
            checkRequestTimeout: "Проверка марки в ЧЗ, сек",
            checkInternetConnectionTimeout: "Проверка доступа в интернет, сек",

        };
    }

    loadConfig(config) {
        if (config?.httpRequestTimeouts) {
            this.cdnLoadTimeout = config.httpRequestTimeouts.cdnRequestTimeout;
            this.checkRequestTimeout = config.httpRequestTimeouts.checkMarkRequestTimeout;
            this.checkInternetConnectionTimeout = config.httpRequestTimeouts.checkInternetConnectionTimeout;
        }

        return this;
    }

    render() {
        var elements = [];

        elements.push(
            Label("lTimeoutConfig", this.LABELS.title),
        );

        elements.push(
            {
                padding: padding,
                rows: [
                    Number(this.LABELS.cdnLoadTimeout, "httpRequestTimeouts.cdnRequestTimeout", this.cdnLoadTimeout, "1111"),
                    Number(this.LABELS.checkRequestTimeout, "httpRequestTimeouts.checkMarkRequestTimeout", this.checkRequestTimeout, "1111"),
                    Number(this.LABELS.checkInternetConnectionTimeout, "httpRequestTimeouts.checkInternetConnectionTimeout", this.checkInternetConnectionTimeout, "1111"),
                ]
            }
        );

        return { id: this.id, rows: elements };
    }
}

export default function (id, config) {
    return new TimeoutsConfigurationElement(id)
        .loadConfig(config)
        .render();
}
