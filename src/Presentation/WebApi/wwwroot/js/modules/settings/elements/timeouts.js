import { Label, Number, padding, CheckBox } from "../../../utils/ui.js";
import { timeoutSecondsValidation, TIMEOUT_SECONDS_MAX } from "../../../utils/validators.js";
import { saveConfiguration } from "../../../services/ConfigurationService.js";

class TimeoutsConfigurationElement {
    constructor(id) {
        this.id = id;
        this.SETTINGS_ID = "timeoutsSettings";
        this.LABELS = {
            title: "Таймауты",
            cdnLoadTimeout: `Загрузка списка cdn, сек (макс. ${TIMEOUT_SECONDS_MAX})`,
            checkRequestTimeout: `Проверка марки в ЧЗ, сек (макс. ${TIMEOUT_SECONDS_MAX})`,
            checkInternetConnectionTimeout: "Проверка доступа в интернет, сек",
            syncWithTsPiot: "Синхронизировать таймауты с ТС ПИОТ",
            pushToTsPiot: "Отправить таймауты в ТС ПИоТ",
            pushSuccess: "Настройки отправлены в ТС ПИоТ",
            pushNoChanges: "Настройки уже совпадают либо нет подключенных ТС ПИоТ",
            pushError: "Ошибка отправки настроек в ТС ПИоТ",
        };
    }

    loadConfig(config) {
        if (config?.httpRequestTimeouts) {
            this.cdnLoadTimeout = config.httpRequestTimeouts.cdnRequestTimeout;
            this.checkRequestTimeout = config.httpRequestTimeouts.checkMarkRequestTimeout;
            this.checkInternetConnectionTimeout = config.httpRequestTimeouts.checkInternetConnectionTimeout;
            this.syncWithTsPiot = config.httpRequestTimeouts.syncWithTsPiot ?? true;
        }

        this.tsPiotEnabled = config?.serverConfig?.tsPiotEnabled ?? false;

        return this;
    }

    render() {
        var elements = [];

        elements.push(
            Label("lTimeoutConfig", this.LABELS.title),
        );

        elements.push(
            {
                padding: padding,
                rows: [
                    Number(this.LABELS.cdnLoadTimeout, "httpRequestTimeouts.cdnRequestTimeout", this.cdnLoadTimeout, "11", timeoutSecondsValidation),
                    Number(this.LABELS.checkRequestTimeout, "httpRequestTimeouts.checkMarkRequestTimeout", this.checkRequestTimeout, "11", timeoutSecondsValidation),
                    Number(this.LABELS.checkInternetConnectionTimeout, "httpRequestTimeouts.checkInternetConnectionTimeout", this.checkInternetConnectionTimeout, "1111"),
                    CheckBox(this.LABELS.syncWithTsPiot, "httpRequestTimeouts.syncWithTsPiot", { value: this.syncWithTsPiot }),
                    {
                        view: "button",
                        id: "pushPiotSettingsButton",
                        type: "icon",
                        icon: "wxi-sync",
                        label: this.LABELS.pushToTsPiot,
                        value: this.LABELS.pushToTsPiot,
                        inputWidth: 280,
                        inputHeight: 40,
                        disabled: !this.tsPiotEnabled,
                        click: () => this.pushSettings()
                    },
                ]
            }
        );

        return { id: this.id, rows: elements };
    }

    async pushSettings() {
        const button = $$("pushPiotSettingsButton");
        const form = button?.getFormView();
        if (!form)
            return;

        try {
            const packet = await saveConfiguration(form.config.id);
            if (!packet?.isSuccess)
                return;

            button.disable();

            const response = await fetch("/api/tspiot/settings", { method: "POST" });
            const data = await response.json().catch(() => ({}));

            if (response.ok) {
                if (data.updated === 0 && data.failed > 0) {
                    webix.message({
                        text: data.errors?.[0] || this.LABELS.pushError,
                        type: "error"
                    });
                    return;
                }

                webix.message({
                    text: this.successMessage(data),
                    type: "success"
                });
                return;
            }

            webix.message({
                text: data.error || this.LABELS.pushError,
                type: "error"
            });
        } catch (error) {
            webix.message({
                text: error.message || this.LABELS.pushError,
                type: "error"
            });
        } finally {
            const enabled = $$("serverConfig.tsPiotEnabled")?.getValue();
            if (enabled)
                button.enable();
            else
                button.disable();
        }
    }

    successMessage(data) {
        if (data.updated > 0 && data.failed > 0)
            return `${this.LABELS.pushSuccess}. Обновлено: ${data.updated}, ошибок: ${data.failed}`;

        if (data.updated > 0)
            return `${this.LABELS.pushSuccess}. Обновлено инстансов: ${data.updated}`;

        return this.LABELS.pushNoChanges;
    }
}

export default function (id, config) {
    return new TimeoutsConfigurationElement(id)
        .loadConfig(config)
        .render();
}
