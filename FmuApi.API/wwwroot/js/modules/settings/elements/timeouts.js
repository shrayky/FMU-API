import { Label, TextBox, Number, padding, CheckBox } from "../../../utils/ui.js";

class TimeoutsConfigurationElement {
    constructor(id) {
        this.id = id;
        this.SETTINGS_ID = "timeoutsSettings";
        this.LABELS = {
            title: "Таймауты",
            cdnLoadTomeout: "Загрузка списка cdn",
            checkRequestTimeout: "Проверка марки в ЧЗ",
            сheckInternetConnectionTimeout: "Проверка доступа в интернет",
        };
    }

    loadConfig(config) {
        if (config?.httpRequestTimeouts) {
            this.cdnLoadTomeout = config.httpRequestTimeouts.cdnRequestTimeout;
            this.checkRequestTimeout = config.httpRequestTimeouts.checkMarkRequestTimeout;
            this.сheckInternetConnectionTimeout = config.httpRequestTimeouts.checkInternetConnectionTimeout;
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
                    Number(this.LABELS.cdnLoadTomeout, "httpRequestTimeouts.cdnRequestTimeout", this.cdnLoadTomeout, "1111"),
                    Number(this.LABELS.checkRequestTimeout, "httpRequestTimeouts.checkMarkRequestTimeout", this.checkRequestTimeout, "1111"),
                    Number(this.LABELS.сheckInternetConnectionTimeout, "httpRequestTimeouts.checkInternetConnectionTimeout", this.сheckInternetConnectionTimeout, "1111"),
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

// export function timeoutsConfigurationElement(id) {
//     var elements = [];

//     elements.push(
//         Label("lTimeoutConfig", "Настройка таймаутов")
//     );

//     elements.push(
//         {
//             view: "subform",
//             name: "httpRequestTimeouts",
//             padding: padding,
//             elements: [
//                 TextBox("number", "Загрузка списка cdn", "cdnRequestTimeout", { "format": "1111" }),
//                 TextBox("number", "Проверка марки в ЧЗ", "checkMarkRequestTimeout", { "format": "1111" }),
//                 TextBox("number", "Проверка доступа в интернет", "checkInternetConnectionTimeout", { "format": "1111" }),
//             ]
//         }
//     );

//     return { id: id, rows: elements }
// }
