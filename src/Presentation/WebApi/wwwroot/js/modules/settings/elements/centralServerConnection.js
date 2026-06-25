import { Number, Label, padding, CheckBox, Text, TableToolbar } from "../../../utils/ui.js";
import { httpAddressValidation } from "../../../utils/validators.js";

const ITALIC_SMALL_STYLE = { "font-style": "italic", "font-size": "smaller" };
const TIME_FORMAT_24H = "%H:%i";

class CentralServerConnectionElement {
    constructor(id) {
        this.id = id;
        this.SETTINGS_ID = "serverSettings";
        this.SCHEDULER_SETTINGS_ID = "schedulerUpdateInstallSettings";
        this.SCHEDULER_TABLE_ID = "SchedulerUpdateInstall";
        this.SCHEDULER_FORM_NAME = "SchedulerUpdateInstallForm";
        this.LABELS = {
            title: "Настройка подключения к сервису мониторинга",
            enabled: "Использовать",
            address: "Веб-адрес сервиса",
            token: "Токен",
            secret: "Секретный ключ",
            interval: "Интервал обмена (минут)",
            downloadNewVersion: "Загружать и устанавливать новую версию",
            schedulerTitle: "Расписание установки обновлений",
            schedulerTip: "если список пуст, обновление устанавливается в любое время",
            newInterval: "Новый интервал",
            editInterval: "Интервал",
            beginTime: "Начало",
            endTime: "Окончание",
            add: "Сохранить",
            close: "Закрыть",
            timeRequired: "Укажите время начала и окончания интервала",
            invalidInterval: "Время начала должно быть меньше времени окончания",
            doExchangeWithServer: "Выполнить обмен",
            addressTip: "⚠️ можно указать несколько адресов через точку с запятой",
        };
    }

    /// Преобразует строку времени в объект Date для datepicker.
    _parseTimeString(timeStr) {
        if (!timeStr)
            return new Date(2000, 0, 1, 0, 0, 0);

        const parts = String(timeStr).split(":");
        const hours = parseInt(parts[0], 10) || 0;
        const minutes = parseInt(parts[1], 10) || 0;
        const seconds = parseInt(parts[2], 10) || 0;

        return new Date(2000, 0, 1, hours, minutes, seconds);
    }

    /// Создаёт поле выбора времени в 24-часовом формате.
    _createTimePicker(id, label, defaultValue) {
        return {
            view: "datepicker",
            type: "time",
            format: TIME_FORMAT_24H,
            editable: true,
            suggest: {
                type: "calendar",
                padding: 0,
                body: {
                    type: "time",
                    calendarTime: TIME_FORMAT_24H,
                    width: 250,
                    height: 240,
                }
            },
            label: label,
            labelPosition: "top",
            id: id,
            name: id,
            value: defaultValue,
        };
    }

    /// Форматирует время для сохранения в формате TimeOnly.
    _formatTimeForSave(value) {
        if (!value)
            return "00:00:00";

        const date = value instanceof Date ? value : new Date(value);
        const hours = date.getHours().toString().padStart(2, "0");
        const minutes = date.getMinutes().toString().padStart(2, "0");
        const seconds = date.getSeconds().toString().padStart(2, "0");

        return `${hours}:${minutes}:${seconds}`;
    }

    loadConfig(config) {
        if (config?.fmuApiCentralServer) {
            const settings = config.fmuApiCentralServer;

            this.enabled = settings.enabled;
            this.address = settings.address;
            this.token = settings.token;
            this.secret = settings.secret;
            this.interval = settings.exchangeRequestInterval;
            this.downloadNewVersion = settings.downloadNewVersion;
            this.schedulerUpdateInstall = settings.schedulerUpdateInstall ?? [];
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
                    CheckBox(this.LABELS.enabled, "fmuApiCentralServer.enabled", {
                        value: this.enabled,
                        on: {
                            onChange: function (enabled) {
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
                        disabled: !this.enabled,
                        rows: [
                            Text(this.LABELS.address,
                                "fmuApiCentralServer.address",
                                this.address,
                                httpAddressValidation),

                            Label("lAddressTip", this.LABELS.addressTip),

                            Text(this.LABELS.token,
                                "fmuApiCentralServer.token",
                                this.token),

                            Text(this.LABELS.secret,
                                "fmuApiCentralServer.secret",
                                this.secret),

                            Number(this.LABELS.interval,
                                "fmuApiCentralServer.exchangeRequestInterval",
                                this.interval),

                            CheckBox(this.LABELS.downloadNewVersion, "fmuApiCentralServer.downloadNewVersion", {
                                value: this.downloadNewVersion,
                                on: {
                                    onChange: (enabled) => {
                                        if (enabled) {
                                            $$(this.SCHEDULER_SETTINGS_ID).enable();
                                        }
                                        else {
                                            $$(this.SCHEDULER_SETTINGS_ID).disable();
                                        }
                                    }
                                }
                            }),

                            {
                                id: this.SCHEDULER_SETTINGS_ID,
                                disabled: !this.downloadNewVersion,
                                rows: [
                                    Label("lSchedulerUpdateInstall", this.LABELS.schedulerTitle),
                                    Label("lSchedulerUpdateInstallTip", this.LABELS.schedulerTip, {
                                        css: ITALIC_SMALL_STYLE
                                    }),
                                    TableToolbar(this.SCHEDULER_TABLE_ID),
                                    this._createSchedulerTable()
                                ]
                            },

                            {
                                view: "button",
                                value: this.LABELS.doExchangeWithServer,
                                css: "webix_primary",
                                click: function () {
                                    webix.ajax()
                                        .get("/api/centralServer/centralServerExchange")
                                        .then(function (response) {
                                            webix.message({
                                                text: "Обмен с центральным сервером выполнен успешно",
                                                type: "success"
                                            });
                                        })
                                        .fail(function (xhr) {
                                            webix.message({
                                                text: "Ошибка обмена с центральным сервером: " + xhr.responseText,
                                                type: "error"
                                            });
                                        });
                                }
                            }
                        ],
                    }
                ]
            }
        );

        return { id: this.id, rows: elements };
    }

    _createSchedulerTable() {
        return {
            view: "formtable",
            id: this.SCHEDULER_TABLE_ID,
            name: "fmuApiCentralServer.schedulerUpdateInstall",
            data: this.schedulerUpdateInstall,
            resizeColumn: true,
            resizeRow: true,
            select: true,
            minHeight: 200,
            columns: [
                { id: "id", header: "Код", hidden: true },
                { id: "beginTime", header: this.LABELS.beginTime, fillspace: true },
                { id: "endTime", header: this.LABELS.endTime, fillspace: true },
            ],
            on: {
                onAfterSelect: () => {
                    $$(`delete_${this.SCHEDULER_TABLE_ID}`).enable();
                },
                onAfterDelete: () => {
                    $$(`delete_${this.SCHEDULER_TABLE_ID}`).disable();
                    if ($$(this.SCHEDULER_TABLE_ID).count() == 0) {
                        $$(`deleteAll_${this.SCHEDULER_TABLE_ID}`).disable();
                    }
                },
                onBeforeAdd: (id, obj) => {
                    if (obj.beginTime == undefined) {
                        this._showSchedulerForm(this.LABELS.newInterval);
                        return false;
                    }
                },
                onItemDblClick: (id) => {
                    this._showSchedulerForm(this.LABELS.editInterval, id);
                }
            }
        };
    }

    _showSchedulerForm(label, id) {
        const windowInnerWidth = window.innerWidth;

        webix.ui({
            view: "window",
            id: this.SCHEDULER_FORM_NAME,
            position: "center",
            modal: true,
            move: false,
            resize: false,
            width: windowInnerWidth * 0.5,
            head: this._createSchedulerFormHeader(label),
            body: this._createSchedulerFormBody(id)
        }).show();

        this._initSchedulerFormValues(id);
        $$("schedulerBeginTime").focus();
    }

    _createSchedulerFormHeader(label) {
        return {
            view: "toolbar",
            elements: [
                {
                    view: "label",
                    label: label,
                },
                {
                    view: "icon",
                    icon: "wxi-close",
                    click: () => $$(this.SCHEDULER_FORM_NAME).close()
                }
            ]
        };
    }

    _createSchedulerFormBody(rowId) {
        return {
            rows: [
                {
                    cols:
                        [this._createTimePicker(
                            "schedulerBeginTime",
                            this.LABELS.beginTime,
                            new Date(2000, 0, 1, 0, 0, 0)
                        ),
                        this._createTimePicker(
                            "schedulerEndTime",
                            this.LABELS.endTime,
                            new Date(2000, 0, 1, 23, 59, 0)
                        ),
                        ]
                },
                {
                    view: "text",
                    type: "number",
                    id: "schedulerRowId",
                    name: "schedulerRowId",
                    hidden: true,
                    value: rowId ?? ""
                },
                {
                    cols: [
                        {
                            view: "button",
                            value: this.LABELS.add,
                            id: "schedulerAddButton",
                            autowidth: "false",
                            width: 400,
                            click: () => this._handleSchedulerAddButton(rowId)
                        },
                        {
                            view: "button",
                            value: this.LABELS.close,
                            id: "schedulerCloseBtn",
                            autowidth: "false",
                            width: 400,
                            click: () => $$(this.SCHEDULER_FORM_NAME).close()
                        },
                        {}
                    ]
                }
            ]
        };
    }

    _handleSchedulerAddButton(rowId) {
        const beginTimeValue = $$("schedulerBeginTime").getValue();
        const endTimeValue = $$("schedulerEndTime").getValue();

        if (!beginTimeValue || !endTimeValue) {
            webix.message({
                text: this.LABELS.timeRequired,
                type: "error"
            });
            return;
        }

        const beginTime = this._formatTimeForSave(beginTimeValue);
        const endTime = this._formatTimeForSave(endTimeValue);

        if (beginTime >= endTime) {
            webix.message({
                text: this.LABELS.invalidInterval,
                type: "error"
            });
            return;
        }

        const table = $$(this.SCHEDULER_TABLE_ID);
        if (!table)
            return;

        if (rowId == undefined) {
            const lastId = table.getLastId();
            const newId = lastId == undefined ? 1 : lastId + 1;
            table.add({ id: newId, beginTime, endTime });
        }
        else {
            table.updateItem(rowId, { id: rowId, beginTime, endTime });
        }

        if (table.count() > 0)
            $$(`deleteAll_${this.SCHEDULER_TABLE_ID}`).enable();

        $$(this.SCHEDULER_FORM_NAME).close();
    }

    _initSchedulerFormValues(rowId) {
        if (rowId == undefined)
            return;

        const table = $$(this.SCHEDULER_TABLE_ID);
        const item = table.getItem(rowId);

        $$("schedulerBeginTime").setValue(this._parseTimeString(item.beginTime));
        $$("schedulerEndTime").setValue(this._parseTimeString(item.endTime));
        $$("schedulerRowId").setValue(item.id);
    }
}

export default function (id, config) {
    return new CentralServerConnectionElement(id)
        .loadConfig(config)
        .render();
}
