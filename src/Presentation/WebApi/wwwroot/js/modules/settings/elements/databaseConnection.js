import { Label, Text, Number, PasswordBox, padding, CheckBox } from "../../../utils/ui.js";
import { couchDbNameValidation, httpAddressValidation } from "../../../utils/validators.js";

class DatabaseConnectionConfigurationElement {
    constructor(id) {
        this.id = id;
        this.SETTINGS_ID = "databaseConnection";
        this.LABELS = {
            title: "База данных",
            enable: "Использовать",
            serverDbAddress: "Адрес сервера CouchDb",
            user: "Пользователь",
            password: "Пароль",
            dbMarks: "База данных марок",
            dbCashDocs: "База документов кассы",
            dbAlcoStamps: "База марок алкоголя",
            bulkBatchSize: "Размер пакета",
            bulkParallelTasks: "Количество параллельных задач",
            bulkLabel: "Параметры пакетной обработки"
        };
    }

    loadConfig(config) {
        if (config?.logging) {
            this.serverDbAddress = config.database.netAddress;
            this.userName = config.database.userName;
            this.userPassword = config.database.password;
            this.marksStateDbName = config.database.marksStateDbName;
            this.frontolDocumentsDbName = config.database.frontolDocumentsDbName;
            this.alcoStampsDbName = config.database.alcoStampsDbName;
            this.enable = config.database.enable;
            this.bulkBatchSize = config.database.bulkBatchSize;
            this.bulkParallelTasks = config.database.bulkParallelTasks;
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
                    CheckBox(this.LABELS.enable, "database.enable", {
                        value: this.enable,
                        on: {
                            onChange: (enabled) => {
                                if (enabled) {
                                    $$(this.SETTINGS_ID).enable();
                                }
                                else {
                                    $$(this.SETTINGS_ID).disable();
                                }
                            }
                        }
                    }),
                    {
                        id: this.SETTINGS_ID,
                        disabled: !this.enable,
                        rows: [
                            Text(this.LABELS.serverDbAddress, "database.netAddress", this.serverDbAddress, httpAddressValidation),
                            {
                                cols: [
                                    Text(this.LABELS.user, "database.userName", this.userName),
                                    PasswordBox(this.LABELS.password, "database.password", { value: this.userPassword })
                                ]
                            },
                            Text(this.LABELS.dbMarks, "database.marksStateDbName", this.marksStateDbName, couchDbNameValidation),
                            Text(this.LABELS.dbCashDocs, "database.frontolDocumentsDbName", this.frontolDocumentsDbName, couchDbNameValidation),
                            Text(this.LABELS.dbAlcoStamps, "database.alcoStampsDbName", this.alcoStampsDbName, couchDbNameValidation),
                            Label("lBulkConfig", this.LABELS.bulkLabel),
                            {
                                padding: padding,
                                cols: [
                                    Number(this.LABELS.bulkBatchSize, "database.bulkBatchSize", this.bulkBatchSize),
                                    Number(this.LABELS.bulkParallelTasks, "database.bulkParallelTasks", this.bulkParallelTasks),
                                ]
                            },
                        ]
                    }
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
                    let address = $$("database.netAddress").getValue();

                    if (address != "")
                        window.open(`${address}/_utils`, "_blank").focus();
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