import { Label, Number, CheckBox, padding } from "../../../utils/ui.js";

class LoggingConfigurationElement {
    constructor(id) {
        this.id = id;
        this.SETTINGS_ID = "loggingSettings";
        this.LABELS = {
            title: "Логирование",
            enabled: "Использовать",
            depth: "Сколько дней хранить файлы лога",
            level: "Уровень логирования"
        };
    }

    LOG_LEVELS = ["Verbose", "Debug", "Information", "Warning", "Error", "Fatal"];

    loadConfig(config) {
        if (config?.logging) {
            this.enabled = config.logging.isEnabled;
            this.level = config.logging.logLevel;
            this.depth = config.logging.logDepth;
        }

        return this;
    }

    render() {
        var elements = [];

        elements.push(
            Label("lLoggingConfig", this.LABELS.title),
        );

        elements.push(
            {
                padding: padding,
                rows: [
                    CheckBox(this.LABELS.enabled, "logging.isEnabled", {
                        value: this.enabled,
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
                        disabled: !this.enabled,
                        rows: [
                            Number(this.LABELS.depth, "logging.logDepth", this.depth, "1111"),
                            
                            {
                                view: "select",
                                label: this.LABELS.level,
                                labelPosition: "top",
                                id: "LogLevel",
                                name: "logging.logLevel",
                                options: this.LOG_LEVELS,
                                value: this.level
                            },
                        ]
                    },
                ]
            }
        );

        return { id: this.id, rows: elements };
    }
}

export default function (id, config) {
    return new LoggingConfigurationElement(id)
        .loadConfig(config)
        .render();
}