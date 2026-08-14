import { Number, padding, CheckBox } from "../../../utils/ui.js";

class StatisticsSettingsElement {
    constructor(id) {
        this.id = id;
        this.LABELS = {
            saveToDb: "Сохранять статистику",
            clearStorageOfStatistics: "Очищать хранилище статистики",
            depthOfStorageOfStatisticsInDays: "Глубина хранения статистики (дней)",
            clearDatabase: "Очистить базу статистики",
            clearDatabaseConfirm: "Все записи статистики будут удалены. Продолжить?",
            clearDatabaseSuccess: "База статистики очищена",
            clearDatabaseError: "Ошибка очистки базы статистики",
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
                        {
                            view: "button",
                            id: "clearStatisticsDb",
                            type: "icon",
                            icon: "wxi-trash",
                            css: "webix_danger",
                            label: this.LABELS.clearDatabase,
                            value: this.LABELS.clearDatabase,
                            inputWidth: 250,
                            inputHeight: 40,
                            click: () => this.clearDatabase()
                        },
                    ]
                }
            ]
        };
    }

    clearDatabase() {
        webix.confirm({
            title: "Подтверждение",
            text: this.LABELS.clearDatabaseConfirm,
            ok: "Да",
            cancel: "Нет",
        }).then(async () => {
            const button = $$("clearStatisticsDb");

            if (button)
                button.disable();

            try {
                const response = await fetch("/api/statistics/clear", { method: "POST" });
                const data = await response.json().catch(() => ({}));

                if (response.ok) {
                    webix.message({
                        text: this.LABELS.clearDatabaseSuccess,
                        type: "success"
                    });
                    return;
                }

                webix.message({
                    text: data.error || this.LABELS.clearDatabaseError,
                    type: "error"
                });
            } catch (error) {
                webix.message({
                    text: error.message || this.LABELS.clearDatabaseError,
                    type: "error"
                });
            } finally {
                if (button)
                    button.enable();
            }
        });
    }
}

export default (id, config) => new StatisticsSettingsElement(id).loadConfig(config).render();
