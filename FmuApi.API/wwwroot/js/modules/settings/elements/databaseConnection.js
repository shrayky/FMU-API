import { Label, Text, PasswordBox, padding } from "../../../utils/ui.js";
import { couchDbNameValidation, httpAddressValidation } from "../../../utils/validators.js";

class DatabaseConnectionConfigurationElement {
    constructor(id) {
        this.id = id;
        this.SETTINGS_ID = "loggingSettings";
        this.LABELS = {
            title: "База данных",
            serverDbAddres: "Адрес сервера CouchDb",
            user: "Пользователь",
            password: "Пароль",
            dbMarks: "База данных марок",
            dbCashDocs: "База документов кассы",
            dbAlcoStamps: "База марок алкоголя"
        };
    }

    loadConfig(config) {
        if (config?.logging) {
            this.serverDbAddres = config.database.netAdres;
            this.userName = config.database.userName;
            this.userPassword = config.database.password;
            this.marksStateDbName = config.database.marksStateDbName;
            this.frontolDocumentsDbName = config.database.frontolDocumentsDbName;
            this.alcoStampsDbName = config.database.alcoStampsDbName;
        }

        return this;
    }

    render() {
        var elements = [];

        elements.push(
            Label("lDatabaseConfig", this.LABELS.title),
        );

        elements.push(
            {
                padding: padding,
                rows: [
                    Text(this.LABELS.serverDbAddres, "database.netAdres", this.serverDbAddres, httpAddressValidation),
                    {
                        cols: [
                            Text(this.LABELS.user, "database.userName", this.userName),
                            PasswordBox(this.LABELS.password, "database.password", {value: this.userPassword})
                        ]
                    },
                    Text(this.LABELS.dbMarks, "database.marksStateDbName", this.marksStateDbName, couchDbNameValidation),
                    Text(this.LABELS.dbCashDocs, "database.frontolDocumentsDbName", this.frontolDocumentsDbName, couchDbNameValidation),
                    Text(this.LABELS.dbAlcoStamps, "database.alcoStampsDbName", this.alcoStampsDbName, couchDbNameValidation),
                ]
            }
        );

        elements.push(
            {
                view: "button",
                id: "fauxton_open",
                value: "Открыть Fauxton",
                inputWidth: 180,
                inputHeight: 40,
                click: _ => {
                    let adres = $$("database.netAdres").getValue();
    
                    if (adres != "")
                        window.open(`${adres}/_utils`, "_blank").focus();
                }
            },
        );

        return { id: this.id, rows: elements };
    }
}

export default function (id, config) {
    return new DatabaseConnectionConfigurationElement(id)
        .loadConfig(config)
        .render();
}