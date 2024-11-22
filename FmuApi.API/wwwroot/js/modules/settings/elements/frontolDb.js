import { Label, Text, PasswordBox, padding } from "../../../utils/ui.js";
import { frontolDbValidation } from "../../../utils/validators.js";

class FrontolDatabaseConnectionConfigurationElement {
    constructor(id) {
        this.id = id;
        this.SETTINGS_ID = "frontolDatabaseConnection";
        this.LABELS = {
            title: "Настройка подключения к базе Frontol (для получения группы печати)",
            maingdbPath: "Путь к файлу main.gdb",
            user: "Имя пользователя firebird",
            password: "Пароль пользователя firebird",
        };
    }

    loadConfig(config) {
        if (config?.frontolAlcoUnit) {
            this.maingdbPath = config.frontolConnectionSettings.path;
            this.user = config.frontolConnectionSettings.userName;
            this.password = config.frontolConnectionSettings.password;
        }

        return this;
    }

    render() {
        var elements = [];

        elements.push(
            Label("lFrontolConnection", this.LABELS.title),
        );

        elements.push(
            {
                padding: padding,
                rows: [
                    Text(this.LABELS.maingdbPath, "frontolConnectionSettings.path", this.maingdbPath, frontolDbValidation),
                    Text(this.LABELS.user, "frontolConnectionSettings.UserName", this.user, {placeholder: "SYSDBA"}),
                    PasswordBox(this.LABELS.password, "frontolConnectionSettings.password", {value: this.password}),
                ]
            }
        );

        return { id: this.id, rows: elements };
    }
}

export default function (id, config) {
    return new FrontolDatabaseConnectionConfigurationElement(id)
        .loadConfig(config)
        .render();
}