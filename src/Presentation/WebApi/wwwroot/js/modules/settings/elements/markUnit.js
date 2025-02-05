import { Label, Text, PasswordBox, padding } from "../../../utils/ui.js";
import { httpAddressValidation } from "../../../utils/validators.js";

class MarkUnitProxyConfigurationElement {
    constructor(id) {
        this.id = id;
        this.SETTINGS_ID = "markUnitConnection";
        this.LABELS = {
            title: "Настройка подключения к MarkUnit",
            addres: "Адрес",
            user: "Имя пользователя",
            password: "Пароль",
        };
    }

    loadConfig(config) {
        if (config?.frontolAlcoUnit) {
            this.addres = config.frontolAlcoUnit.netAdres;
            this.user = config.frontolAlcoUnit.userName;
            this.password = config.frontolAlcoUnit.password;
        }

        return this;
    }

    render() {
        var elements = [];

        elements.push(
            Label("lMarkUnit", this.LABELS.title),
        );

        elements.push(
            {
                padding: padding,
                rows: [
                    Text(this.LABELS.addres, "frontolAlcoUnit.netAdres", this.addres, httpAddressValidation),
                    Text(this.LABELS.user, "frontolAlcoUnit.UserName", this.user),
                    PasswordBox(this.LABELS.password, "frontolAlcoUnit.password", {value: this.password}),
                ]
            }
        );

        return { id: this.id, rows: elements };
    }
}

export default function (id, config) {
    return new MarkUnitProxyConfigurationElement(id)
        .loadConfig(config)
        .render();
}