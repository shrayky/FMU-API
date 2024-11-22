import { Label, Text, CheckBox, padding } from "../../../utils/ui.js";
import { windowsPathValidation } from "../../../utils/validators.js";

class AutoUpdateElement {
    constructor(id) {
        this.id = id;
        this.SETTINGS_ID = "autoUpdateSettings";
        this.LABELS = {
            title: "Автоматическое обновление",
            enabled: "Использовать",
            catalog: "Каталог с файлами обновления",
            hours: "Часы для автообновления"
        };
        this.HOURS_COUNT = 24;
        this.TIME_COMBO_WIDTH = 60;
    }

    createHoursOptions() {
        return Array.from({ length: this.HOURS_COUNT }, (_, index) => ({
            id: index + 1,
            value: index.toString().padStart(2, '0')
        }));
    }

    timeComboBox(id, name, value = 1) {
        return {
            view: "combo",
            id,
            name,
            value: value,
            options: this.createHoursOptions(),
            maxWidth: this.TIME_COMBO_WIDTH,
            format: "02",
            newValues: false,
        };
    }

    loadConfig(config) {
        if (config?.autoUpdate) {
            this.enabled = config.autoUpdate.enabled ?? false;
            this.updateFilesCatalog = config.autoUpdate.updateFilesCatalog ?? "";
            this.fromHour = config?.autoUpdate?.fromHour ?? 0;
            this.untilHour = config?.autoUpdate?.untilHour ?? 0;
        }
        return this;
    }

    render() {
        var elements = [];

        elements.push(
            Label("lAutoupdateLabel", this.LABELS.title),
        );

        elements.push(
            {
                padding: padding,
                rows: [
                    CheckBox(this.LABELS.enabled, "autoUpdate.enabled", {
                        //"id": "enabled",
                        value: this.enabled,
                        on: {
                            onChange: (enabled) => {
                                if (enabled) {
                                    $$(this.SETTINGS_ID).enable();
                                } else {
                                    $$(this.SETTINGS_ID).disable();
                                }
                            }
                        }
                    }),

                    {
                        id: this.SETTINGS_ID,
                        disabled: !this.enabled,
                        rows: [
                            Text(this.LABELS.catalog,
                                 "autoUpdate.updateFilesCatalog",
                                 this.updateFilesCatalog,
                                 windowsPathValidation),

                            Label("lAutoupdateHours", this.LABELS.hours),

                            {
                                padding: { left: 20, },
                                cols: [
                                    this.timeComboBox("fromHour", "autoUpdate.fromHour", this.fromHour + 1),
                                    Label("AutoupdateTimeSeparator", "-", { "maxWidth": 5 }),
                                    this.timeComboBox("untilHour", "autoUpdate.untilHour", this.untilHour + 1),
                                    {}
                                ]
                            },
                        ],
                    }
                ],
            }
        );

        return { id: this.id, rows: elements };
    }
}

export default function (id, config) {
    return new AutoUpdateElement(id)
        .loadConfig(config)
        .render();
}