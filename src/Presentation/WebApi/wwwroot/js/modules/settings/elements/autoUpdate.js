import { Label, Text, CheckBox, Number, padding } from "../../../utils/ui.js";
import { windowsPathValidation } from "../../../utils/validators.js";

const ITALIC_SMALL_STYLE = { "font-style": "italic", "font-size": "smaller" };

class AutoUpdateElement {
    constructor(id) {
        this.id = id;
        this.SETTINGS_ID = "autoUpdateSettings";
        this.LABELS = {
            title: "Автоматическое обновление",
            enabled: "Использовать",
            catalog: "Каталог с файлами обновления",
            hours: "Часы для автоматического обновления",
            fileNameDescription: "служба ищет в указанном каталоге архив update_{архитектура}_{операционная система}.zip",
            fileNameExample: "какие должны быть имена файлов обновления: update_x64_win.zip, update_x64_linux.zip, update_x86_win.zip",
            timeIntervalDescription: "если правая граница интервала 23:59, то ставьте значение 00",
            checkUpdateInterval: "Интервал проверки наличия файла обновления (минуты)",
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
                
            var fromHour = parseInt(config?.autoUpdate?.fromHour);
            fromHour = fromHour <= 0 ? 1: fromHour;

            var untilHour = parseInt(config?.autoUpdate?.untilHour);
            untilHour = untilHour <= 0 ? 24: untilHour;

            this.fromHour = fromHour - 1 || 0;
            this.untilHour = untilHour - 1 || 0;

            this.checkUpdateIntervalMinutes = config?.autoUpdate?.checkUpdateIntervalMinutes ?? 10;
        }
        return this;
    }

    render() {
        var elements = [];

        elements.push(
            Label("lAutoUpdateLabel", this.LABELS.title),
        );

        elements.push(
            {
                padding: padding,
                rows: [
                    CheckBox(this.LABELS.enabled, "autoUpdate.enabled", {
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

                            Label("fileNameDescription", this.LABELS.fileNameDescription,
                                {
                                    css: ITALIC_SMALL_STYLE
                                }
                            ),

                            Label("fileNameExample", this.LABELS.fileNameExample,
                                {
                                    css: ITALIC_SMALL_STYLE
                                }
                            ),

                            Label("lAutoUpdateHours", this.LABELS.hours),

                            {
                                padding: { left: 20, },
                                cols: [
                                    this.timeComboBox("fromHour", "autoUpdate.fromHour", this.fromHour + 1),
                                    Label("AutoUpdateTimeSeparator", "-", { "maxWidth": 5 }),
                                    this.timeComboBox("untilHour", "autoUpdate.untilHour", this.untilHour + 1),
                                    {}
                                ]
                            },
                            
                            Label("timeIntervalDescription", this.LABELS.timeIntervalDescription,
                                {
                                    css: ITALIC_SMALL_STYLE
                                }
                            ),

                            Number(this.LABELS.checkUpdateInterval, "autoUpdate.checkUpdateIntervalMinutes", this.checkUpdateIntervalMinutes),
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