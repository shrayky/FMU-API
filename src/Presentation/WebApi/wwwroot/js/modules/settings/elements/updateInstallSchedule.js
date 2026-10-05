import { TableToolbar } from "../../../utils/ui.js";

const TIME_FORMAT_24H = "%H:%i";

const DEFAULT_LABELS = {
    title: "Расписание установки обновлений",
    tip: "⚠️если список пуст, обновление устанавливается в любое время",
    newInterval: "Новый интервал",
    editInterval: "Интервал",
    beginTime: "Начало",
    endTime: "Окончание",
    add: "Сохранить",
    close: "Закрыть",
    timeRequired: "Укажите время начала и окончания интервала",
    invalidInterval: "Время начала должно быть меньше времени окончания интервала",
};

/// Общий блок «расписание установки обновлений»: таблица интервалов и форма интервала.
/// prefix — префикс id, чтобы вкладки не занимали одни и те же имена.
class UpdateInstallScheduleElement {
    constructor({ prefix, name, data, disabled, labels }) {
        this.name = name;
        this.data = data ?? [];
        this.disabled = disabled;
        this.labels = labels;

        this.SETTINGS_ID = `${prefix}Settings`;
        this.TABLE_ID = `${prefix}Table`;
        this.FORM_ID = `${prefix}Form`;
        this.BEGIN_TIME_ID = `${prefix}BeginTime`;
        this.END_TIME_ID = `${prefix}EndTime`;
        this.ROW_ID = `${prefix}RowId`;
        this.ADD_BUTTON_ID = `${prefix}AddButton`;
        this.CLOSE_BUTTON_ID = `${prefix}CloseButton`;
    }

    render() {
        return {
            id: this.SETTINGS_ID,
            disabled: this.disabled,
            rows: [
                this._labelWithTip(),
                TableToolbar(this.TABLE_ID),
                this._createTable()
            ]
        };
    }

    _labelWithTip() {
        const safeTitle = webix.template.escape(this.labels.title);
        const safeTip = webix.template.escape(this.labels.tip);

        return {
            cols: [
                {
                    view: "label",
                    id: `${this.SETTINGS_ID}Label`,
                    label: this.labels.title,
                    width: 0,
                    autowidth: true,
                },
                {
                    view: "template",
                    id: `${this.SETTINGS_ID}LabelTip`,
                    borderless: true,
                    css: "webix_el_label",
                    template: `<div style="display:flex;align-items:center;height:100%"><span style="font-style:italic;font-size:smaller">${safeTip}</span></div>`
                }
            ]
        };
    }

    _createTable() {
        return {
            view: "formtable",
            id: this.TABLE_ID,
            name: this.name,
            data: this.data,
            resizeColumn: true,
            resizeRow: true,
            select: true,
            minHeight: 200,
            columns: [
                { id: "id", header: "Код", hidden: true },
                { id: "beginTime", header: this.labels.beginTime, fillspace: true },
                { id: "endTime", header: this.labels.endTime, fillspace: true },
            ],
            on: {
                onAfterSelect: () => {
                    $$(`delete_${this.TABLE_ID}`).enable();
                },
                onAfterDelete: () => {
                    $$(`delete_${this.TABLE_ID}`).disable();
                    if ($$(this.TABLE_ID).count() == 0) {
                        $$(`deleteAll_${this.TABLE_ID}`).disable();
                    }
                },
                onBeforeAdd: (id, obj) => {
                    if (obj.beginTime == undefined) {
                        this._showForm(this.labels.newInterval);
                        return false;
                    }
                },
                onItemDblClick: (id) => {
                    this._showForm(this.labels.editInterval, id);
                }
            }
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

    _showForm(label, id) {
        const windowInnerWidth = window.innerWidth;

        webix.ui({
            view: "window",
            id: this.FORM_ID,
            position: "center",
            modal: true,
            move: false,
            resize: false,
            width: windowInnerWidth * 0.5,
            head: this._createFormHeader(label),
            body: this._createFormBody(id)
        }).show();

        this._initFormValues(id);
        $$(this.BEGIN_TIME_ID).focus();
    }

    _createFormHeader(label) {
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
                    click: () => $$(this.FORM_ID).close()
                }
            ]
        };
    }

    _createFormBody(rowId) {
        return {
            rows: [
                {
                    cols:
                        [this._createTimePicker(
                            this.BEGIN_TIME_ID,
                            this.labels.beginTime,
                            new Date(2000, 0, 1, 0, 0, 0)
                        ),
                        this._createTimePicker(
                            this.END_TIME_ID,
                            this.labels.endTime,
                            new Date(2000, 0, 1, 23, 59, 0)
                        ),
                        ]
                },
                {
                    view: "text",
                    type: "number",
                    id: this.ROW_ID,
                    name: this.ROW_ID,
                    hidden: true,
                    value: rowId ?? ""
                },
                {
                    cols: [
                        {
                            view: "button",
                            value: this.labels.add,
                            id: this.ADD_BUTTON_ID,
                            autowidth: "false",
                            width: 400,
                            click: () => this._handleAddButton(rowId)
                        },
                        {
                            view: "button",
                            value: this.labels.close,
                            id: this.CLOSE_BUTTON_ID,
                            autowidth: "false",
                            width: 400,
                            click: () => $$(this.FORM_ID).close()
                        },
                        {}
                    ]
                }
            ]
        };
    }

    _handleAddButton(rowId) {
        const beginTimeValue = $$(this.BEGIN_TIME_ID).getValue();
        const endTimeValue = $$(this.END_TIME_ID).getValue();

        if (!beginTimeValue || !endTimeValue) {
            webix.message({
                text: this.labels.timeRequired,
                type: "error"
            });
            return;
        }

        const beginTime = this._formatTimeForSave(beginTimeValue);
        const endTime = this._formatTimeForSave(endTimeValue);

        if (beginTime >= endTime) {
            webix.message({
                text: this.labels.invalidInterval,
                type: "error"
            });
            return;
        }

        const table = $$(this.TABLE_ID);
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
            $$(`deleteAll_${this.TABLE_ID}`).enable();

        $$(this.FORM_ID).close();
    }

    _initFormValues(rowId) {
        if (rowId == undefined)
            return;

        const table = $$(this.TABLE_ID);
        const item = table.getItem(rowId);

        $$(this.BEGIN_TIME_ID).setValue(this._parseTimeString(item.beginTime));
        $$(this.END_TIME_ID).setValue(this._parseTimeString(item.endTime));
        $$(this.ROW_ID).setValue(item.id);
    }
}

export default function updateInstallSchedule({ prefix, name, data, disabled = false, labels = {} }) {
    return new UpdateInstallScheduleElement({
        prefix,
        name,
        data,
        disabled,
        labels: { ...DEFAULT_LABELS, ...labels },
    }).render();
}
