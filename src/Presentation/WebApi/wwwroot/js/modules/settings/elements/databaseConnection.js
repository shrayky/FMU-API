import { Label, Text, Number, PasswordBox, padding, CheckBox } from "../../../utils/ui.js";
import { httpAddressValidation } from "../../../utils/validators.js";

const COUCH_DB_PROVIDER = "CouchDb";
const SQLITE_PROVIDER = "Sqlite";

const PROVIDER_FIELD = "database.provider";
const SQLITE_PATH_FIELD = "database.sqlitePath";
const COUCH_DB_BLOCK_ID = "couchDbConnectionBlock";
const SQLITE_BLOCK_ID = "sqliteConnectionBlock";
const PARALLEL_TASKS_BLOCK_ID = "bulkParallelTasksBlock";

/// Настройки подключения: выбор СУБД, параметры CouchDB и путь к файлу SQLite.
class DatabaseConnectionConfigurationElement {
    constructor(id) {
        this.id = id;
        this.SETTINGS_ID = "databaseConnection";
        this.LABELS = {
            enable: "Использовать",
            provider: "СУБД",
            serverDbAddress: "Адрес сервера CouchDb",
            user: "Пользователь",
            password: "Пароль",
            sqlitePath: "Файл SQLite",
            bulkBatchSize: "Размер пакета",
            bulkParallelTasks: "Количество параллельных задач",
            bulkLabel: "Параметры пакетной обработки",
            queryLimit: "Максимальное количество записей для запроса выборки",
            queryTimeout: "Таймаут запроса (секунд)",
            disableDbLog: "Отключить лог базы данных",
        };
    }

    loadConfig(config) {
        if (config?.database) {
            const settings = config.database;

            this.enable = settings.enable;

            this.provider = settings.provider ?? COUCH_DB_PROVIDER;
            this.sqlitePath = settings.sqlitePath;

            this.serverDbAddress = settings.netAddress;
            this.userName = settings.userName;
            this.userPassword = settings.password;

            this.bulkBatchSize = settings.bulkBatchSize;
            this.bulkParallelTasks = settings.bulkParallelTasks;
            this.queryLimit = settings.queryLimit;
            this.queryTimeout = settings.queryTimeoutSeconds;
            this.disableDbLog = settings.disableDbLog;
        }

        return this;
    }

    render() {
        let elements = [];

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
                            this._providerSelect(),

                            this._couchDbFields(),

                            this._sqliteFields(),

                            Label("lBulkConfig", this.LABELS.bulkLabel),
                            {
                                padding: padding,
                                cols: [
                                    Number(this.LABELS.bulkBatchSize, "database.bulkBatchSize", this.bulkBatchSize),
                                    {
                                        id: PARALLEL_TASKS_BLOCK_ID,
                                        hidden: this.provider !== COUCH_DB_PROVIDER,
                                        rows: [
                                            Number(this.LABELS.bulkParallelTasks, "database.bulkParallelTasks", this.bulkParallelTasks)
                                        ]
                                    },
                                    Number(this.LABELS.queryTimeout, "database.queryTimeoutSeconds", this.queryTimeout),
                                ]
                            },

                            CheckBox(this.LABELS.disableDbLog, "database.disableDbLog", { value: this.disableDbLog }),
                        ]
                    }
                ]
            }
        );

        return { id: this.id, rows: elements };
    }

    _providerSelect() {
        return {
            view: "richselect",
            id: PROVIDER_FIELD,
            name: PROVIDER_FIELD,
            label: this.LABELS.provider,
            labelPosition: "top",
            value: this.provider,
            options: [
                { id: COUCH_DB_PROVIDER, value: "CouchDB" },
                { id: SQLITE_PROVIDER, value: "SQLite" }
            ],
            on: {
                onChange: (provider) => this._applyProvider(provider)
            }
        };
    }

    _couchDbFields() {
        return {
            id: COUCH_DB_BLOCK_ID,
            hidden: this.provider !== COUCH_DB_PROVIDER,
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
                }
            ]
        };
    }

    _sqliteFields() {
        return {
            id: SQLITE_BLOCK_ID,
            hidden: this.provider !== SQLITE_PROVIDER,
            rows: [
                Text(this.LABELS.sqlitePath, SQLITE_PATH_FIELD, this.sqlitePath)
            ]
        };
    }

    /// Показывает поля выбранной СУБД. Адрес, логин и пароль CouchDB остаются в форме и сохраняются.
    _applyProvider(provider) {
        const couchDbBlock = $$(COUCH_DB_BLOCK_ID);
        const sqliteBlock = $$(SQLITE_BLOCK_ID);
        const parallelTasksBlock = $$(PARALLEL_TASKS_BLOCK_ID);

        if (couchDbBlock)
            provider === COUCH_DB_PROVIDER ? couchDbBlock.show() : couchDbBlock.hide();

        if (sqliteBlock)
            provider === SQLITE_PROVIDER ? sqliteBlock.show() : sqliteBlock.hide();

        if (parallelTasksBlock)
            provider === COUCH_DB_PROVIDER ? parallelTasksBlock.show() : parallelTasksBlock.hide();
    }
}

export default function (id, config) {
    return new DatabaseConnectionConfigurationElement(id)
        .loadConfig(config)
        .render();
}
