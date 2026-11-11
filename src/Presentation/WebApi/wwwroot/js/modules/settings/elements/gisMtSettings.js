import { Number, padding, CheckBox, TimePicker } from "../../../utils/ui.js";
import { parseTime } from "../../../utils/time.js";

class GisMtSettingsElement {
    constructor(id) {
        this.id = id;
        this.LABELS = {
            pollInterval: "Интервал опроса документов (минуты)",
            markRetentionDays: "Срок хранения невалидных марок (дни)",
            documentsSyncDays: "Период загрузки документов (дней, включая текущий)",
            stockLoadEnabled: "Загружать остатки марок ежедневно",
            stockLoadTime: "Время загрузки остатков",
            documentsEnabled: "Использовать документы ГИС МТ",
            autoCreateConnectTap: "Автоматически создавать постановку на кран",
            autoSendConnectTap: "Автоматически отправлять постановку на кран"
        };
    }

    loadConfig(config) {
        const settings = config?.gisMtSettings ?? {};
        this.mtDocumentsPollIntervalMinutes = settings.mtDocumentsPollIntervalMinutes ?? 10;
        this.markRetentionDays = settings.markRetentionDays ?? 365;
        this.documentsSyncDays = settings.documentsSyncDays ?? 1;
        this.stockLoadEnabled = settings.stockLoadEnabled ?? false;
        this.stockLoadTime = parseTime(settings.stockLoadTime || "03:00:00");
        this.documentsEnabled = settings.documentsEnabled ?? false;
        this.autoCreateConnectTap = settings.autoCreateConnectTap ?? false;
        this.autoSendConnectTap = settings.autoSendConnectTap ?? false;
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
                        TimePicker(
                            this.LABELS.stockLoadTime,
                            "gisMtSettings.stockLoadTime",
                            this.stockLoadTime
                        ),
                        CheckBox(
                            this.LABELS.documentsEnabled,
                            "gisMtSettings.documentsEnabled",
                            {
                                value: this.documentsEnabled,
                                on: {
                                    onChange: (enabled) => {
                                        const block = $$("gisMtJournalSettings");

                                        if (block)
                                            enabled ? block.enable() : block.disable();
                                    }
                                }
                            }
                        ),
                        {
                            id: "gisMtJournalSettings",
                            disabled: !this.documentsEnabled,
                            rows: [
                                CheckBox(
                                    this.LABELS.autoCreateConnectTap,
                                    "gisMtSettings.autoCreateConnectTap",
                                    {
                                        value: this.autoCreateConnectTap,
                                        on: {
                                            onChange: (enabled) => {
                                                const block = $$("gisMtConnectTapSendSettings");

                                                if (block)
                                                    enabled ? block.enable() : block.disable();
                                            }
                                        }
                                    }
                                ),
                                {
                                    id: "gisMtConnectTapSendSettings",
                                    disabled: !this.autoCreateConnectTap,
                                    rows: [
                                        CheckBox(
                                            this.LABELS.autoSendConnectTap,
                                            "gisMtSettings.autoSendConnectTap",
                                            { value: this.autoSendConnectTap }
                                        )
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }
}

export default (id, config) => new GisMtSettingsElement(id).loadConfig(config).render();
