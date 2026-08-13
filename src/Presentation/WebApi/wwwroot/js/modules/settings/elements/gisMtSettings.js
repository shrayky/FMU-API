import { Number, padding, CheckBox } from "../../../utils/ui.js";

const TIME_FORMAT_24H = "%H:%i";

class GisMtSettingsElement {
    constructor(id) {
        this.id = id;
        this.LABELS = {
            pollInterval: "Интервал опроса документов (минуты)",
            markRetentionDays: "Срок хранения невалидных марок (дни)",
            documentsSyncDays: "Период загрузки документов (дней, включая текущий)",
            stockLoadEnabled: "Загружать остатки марок ежедневно",
            stockLoadTime: "Время загрузки остатков"
        };
    }

    _parseTimeString(timeStr) {
        if (!timeStr)
            return new Date(2000, 0, 1, 3, 0, 0);

        const parts = String(timeStr).split(":");
        const hours = parseInt(parts[0], 10) || 0;
        const minutes = parseInt(parts[1], 10) || 0;
        const seconds = parseInt(parts[2], 10) || 0;

        return new Date(2000, 0, 1, hours, minutes, seconds);
    }

    _createTimePicker(name, label, defaultValue) {
        return {
            view: "datepicker",
            type: "time",
            format: TIME_FORMAT_24H,
            editable: true,
            suggest: {
                type: "calendar",
                padding: 0,
                body: {
                    type: "timeboard",
                    button: true,
                    twelve: false
                }
            },
            label,
            labelPosition: "top",
            id: name,
            name,
            value: defaultValue,
        };
    }

    loadConfig(config) {
        const settings = config?.gisMtSettings ?? {};
        this.mtDocumentsPollIntervalMinutes = settings.mtDocumentsPollIntervalMinutes ?? 10;
        this.markRetentionDays = settings.markRetentionDays ?? 365;
        this.documentsSyncDays = settings.documentsSyncDays ?? 1;
        this.stockLoadEnabled = settings.stockLoadEnabled ?? false;
        this.stockLoadTime = this._parseTimeString(settings.stockLoadTime ?? "03:00:00");
        return this;
    }

    render() {
        return {
            id: this.id,
            rows: [
                {
                    padding: padding,
                    rows: [
                        Number(
                            this.LABELS.pollInterval,
                            "gisMtSettings.mtDocumentsPollIntervalMinutes",
                            this.mtDocumentsPollIntervalMinutes,
                            "111"
                        ),
                        Number(
                            this.LABELS.documentsSyncDays,
                            "gisMtSettings.documentsSyncDays",
                            this.documentsSyncDays,
                            "111"
                        ),
                        Number(
                            this.LABELS.markRetentionDays,
                            "gisMtSettings.markRetentionDays",
                            this.markRetentionDays,
                            "111"
                        ),
                        CheckBox(
                            this.LABELS.stockLoadEnabled,
                            "gisMtSettings.stockLoadEnabled",
                            { value: this.stockLoadEnabled }
                        ),
                        this._createTimePicker(
                            "gisMtSettings.stockLoadTime",
                            this.LABELS.stockLoadTime,
                            this.stockLoadTime
                        )
                    ]
                }
            ]
        };
    }
}

export default (id, config) => new GisMtSettingsElement(id).loadConfig(config).render();
