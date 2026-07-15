import { Label, TextBox, Number, padding, CheckBox } from "../../../utils/ui.js";
import { timeoutSecondsValidation, TIMEOUT_SECONDS_MAX } from "../../../utils/validators.js";

class TimeoutsConfigurationElement {
    constructor(id) {
        this.id = id;
        this.SETTINGS_ID = "timeoutsSettings";
        this.LABELS = {
            title: "Таймауты",
            cdnLoadTimeout: `Загрузка списка cdn, сек (макс. ${TIMEOUT_SECONDS_MAX})`,
            checkRequestTimeout: `Проверка марки в ЧЗ, сек (макс. ${TIMEOUT_SECONDS_MAX})`,
            checkInternetConnectionTimeout: "Проверка доступа в интернет, сек",
            syncWithTsPiot: "Синхронизировать таймауты с ТС ПИОТ",
        };
    }

    loadConfig(config) {
        if (config?.httpRequestTimeouts) {
            this.cdnLoadTimeout = config.httpRequestTimeouts.cdnRequestTimeout;
            this.checkRequestTimeout = config.httpRequestTimeouts.checkMarkRequestTimeout;
            this.checkInternetConnectionTimeout = config.httpRequestTimeouts.checkInternetConnectionTimeout;
            this.syncWithTsPiot = config.httpRequestTimeouts.syncWithTsPiot ?? true;
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
                    Number(this.LABELS.cdnLoadTimeout, "httpRequestTimeouts.cdnRequestTimeout", this.cdnLoadTimeout, "11", timeoutSecondsValidation),
                    Number(this.LABELS.checkRequestTimeout, "httpRequestTimeouts.checkMarkRequestTimeout", this.checkRequestTimeout, "11", timeoutSecondsValidation),
                    Number(this.LABELS.checkInternetConnectionTimeout, "httpRequestTimeouts.checkInternetConnectionTimeout", this.checkInternetConnectionTimeout, "1111"),
                    CheckBox(this.LABELS.syncWithTsPiot, "httpRequestTimeouts.syncWithTsPiot", { value: this.syncWithTsPiot }),
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
