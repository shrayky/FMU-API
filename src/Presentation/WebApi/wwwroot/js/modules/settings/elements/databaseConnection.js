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
            bulkBatchSize: "Размер пакета",
            bulkParallelTasks: "Количество параллельных задач",
            bulkLabel: "Параметры пакетной обработки",
            queryLimit: "Максимальное количество записей для запроса выборки",
            queryTimeout: "Таймаут запроса (секунд)",
            statisticsStorageLabel: "Хранение статистики",
            clearStorageOfStatistics: "Очищать хранилище статистики",
            depthOfStorageOfStatisticsInDays: "Глубина хранения статистики (дней)",
        };
    }

    loadConfig(config) {
        if (config?.database) {
            const settings = config.database;

            this.enable = settings.enable;
            
            this.serverDbAddress = settings.netAddress;
            this.userName = settings.userName;
            this.userPassword = settings.password;

            this.bulkBatchSize = settings.bulkBatchSize;
            this.bulkParallelTasks = settings.bulkParallelTasks;
            this.queryLimit = settings.queryLimit;
            this.queryTimeout = settings.queryTimeoutSeconds;

            this.clearStorageOfStatistics = settings.clearStorageOfStatistics;
            this.depthOfStorageOfStatisticsInDays = settings.depthOfStorageOfStatisticsInDays;
        }

        return this;
    }

    render() {
        let elements = [];

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

                            Label("lBulkConfig", this.LABELS.bulkLabel),
                            {
                                padding: padding,
                                cols: [
                                    Number(this.LABELS.bulkBatchSize, "database.bulkBatchSize", this.bulkBatchSize),
                                    Number(this.LABELS.bulkParallelTasks, "database.bulkParallelTasks", this.bulkParallelTasks),
                                    Number(this.LABELS.queryTimeout, "database.queryTimeoutSeconds", this.queryTimeout),
                                ]
                            },

                            Label("lStatisticsStorage", this.LABELS.statisticsStorageLabel),
                            {
                                padding: padding,
                                rows: [
                                    CheckBox(this.LABELS.clearStorageOfStatistics, "database.clearStorageOfStatistics", {
                                        value: this.clearStorageOfStatistics,
                                        on: {
                                            onChange: (enabled) => {
                                                if (enabled) {
                                                    $$("statisticsStorageSettings").enable();
                                                }
                                                else {
                                                    $$("statisticsStorageSettings").disable();
                                                }
                                            }
                                        }
                                    }),
                                    {
                                        id: "statisticsStorageSettings",
                                        disabled: !this.clearStorageOfStatistics,
                                        rows: [
                                            Number(
                                                this.LABELS.depthOfStorageOfStatisticsInDays,
                                                "database.depthOfStorageOfStatisticsInDays",
                                                this.depthOfStorageOfStatisticsInDays
                                            ),
                                        ]
                                    },
                                ]
                            },
                        ]
                    }
                ]
            }
        );

        return { id: this.id, rows: elements };
    }
}

export default function (id, config) {
    return new DatabaseConnectionConfigurationElement(id)
        .loadConfig(config)
        .render();
}