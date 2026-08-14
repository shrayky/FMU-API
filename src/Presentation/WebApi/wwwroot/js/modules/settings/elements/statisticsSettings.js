import { Number, padding, CheckBox } from "../../../utils/ui.js";

class StatisticsSettingsElement {
    constructor(id) {
        this.id = id;
        this.LABELS = {
            saveToDb: "Сохранять статистику",
            clearStorageOfStatistics: "Очищать хранилище статистики",
            depthOfStorageOfStatisticsInDays: "Глубина хранения статистики (дней)",
        };
    }

    loadConfig(config) {
        const settings = config?.statistics ?? {};

        this.saveToDb = settings.saveToDb ?? true;
        this.clearStorageOfStatistics = settings.clearStorageOfStatistics ?? true;
        this.depthOfStorageOfStatisticsInDays = settings.depthOfStorageOfStatisticsInDays ?? 30;

        return this;
    }

    render() {
        return {
            id: this.id,
            rows: [
                {
                    padding: padding,
                    rows: [
                        CheckBox(this.LABELS.saveToDb, "statistics.saveToDb", {
                            value: this.saveToDb
                        }),
                        CheckBox(this.LABELS.clearStorageOfStatistics, "statistics.clearStorageOfStatistics", {
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
                                    "statistics.depthOfStorageOfStatisticsInDays",
                                    this.depthOfStorageOfStatisticsInDays
                                ),
                            ]
                        },
                    ]
                }
            ]
        };
    }
}

export default (id, config) => new StatisticsSettingsElement(id).loadConfig(config).render();
