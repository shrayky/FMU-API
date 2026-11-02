import { saveConfiguration } from "../../../services/ConfigurationService.js";
import { CheckBox, Label, Number, Text, padding } from "../../../utils/ui.js";

const STATUS_ID = "tsdAppUpdateStatus";
const SETTINGS_ID = "tsdAppUpdateSettings";
const SECTION_TITLE = "Обновление Fmu-Api-TSD";

class TsdAppUpdateElement {
    constructor(id) {
        this.id = id;

        this.enabled = false;
        this.checkIntervalMinutes = 120;
        this.repositoryUrl = "https://github.com/shrayky/fmu-api-tsd-app";

        this.LABELS = {
            title: SECTION_TITLE,
            enabled: "Использовать",
            repository: "Адрес GitHub",
            checkInterval: "Интервал проверки (минут)",
            download: "Получить",
            storedEmpty: "Пакетов нет",
            storedFailed: "Не удалось прочитать пакеты",
        };
    }

    loadConfig(config) {
        const settings = config?.tsdAppUpdate;

        if (settings) {
            this.enabled = settings.enabled ?? false;
            this.checkIntervalMinutes = settings.checkIntervalMinutes ?? 120;
            this.repositoryUrl = settings.repositoryUrl || "https://github.com/shrayky/fmu-api-tsd-app";
        }

        return this;
    }

    render() {
        const labels = this.LABELS;
        webix.delay(() => showStoredPackages(labels));

        return {
            id: this.id,
            rows: [
                Label("lTsdAppUpdate", this.LABELS.title),
                {
                    padding: padding,
                    rows: [
                        {
                            margin: 8,
                            cols: [
                                Text(this.LABELS.repository, "tsdAppUpdate.repositoryUrl", this.repositoryUrl, {
                                    placeholder: "https://github.com/owner/repo",
                                }),
                                {
                                    width: 148,
                                    rows: [
                                        {},
                                        {
                                            view: "button",
                                            value: this.LABELS.download,
                                            autowidth: false,
                                            width: 140,
                                            click: () => downloadTsdApp(this.LABELS),
                                        },
                                    ],
                                },
                            ],
                        },
                        CheckBox(this.LABELS.enabled, "tsdAppUpdate.enabled", {
                            value: this.enabled,
                            on: {
                                onChange: (enabled) => {
                                    const settings = $$(SETTINGS_ID);
                                    if (enabled)
                                        settings.enable();
                                    else
                                        settings.disable();
                                }
                            }
                        }),
                        {
                            id: SETTINGS_ID,
                            disabled: !this.enabled,
                            rows: [
                                Number(
                                    this.LABELS.checkInterval,
                                    "tsdAppUpdate.checkIntervalMinutes",
                                    this.checkIntervalMinutes
                                ),
                            ],
                        },
                        {
                            view: "template",
                            id: STATUS_ID,
                            autoheight: true,
                            borderless: true,
                            css: { "line-height": "20px" },
                            template: this.LABELS.storedEmpty,
                        },
                    ],
                },
                {}
            ],
        };
    }
}

function packageFileName(item) {
    if (item.fileName)
        return item.fileName;

    return `tsd-${item.versionName}-${item.versionCode}-${item.abi}.apk`;
}

function escapeHtml(value) {
    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;");
}

function downloadLink(item) {
    const href = "/api/tsd/update/apk?abi=" + encodeURIComponent(item.abi);
    const name = escapeHtml(packageFileName(item));
    return `<a href="${href}" download="${name}" title="Скачать" style="margin-left:8px;text-decoration:none;vertical-align:middle"><span class="webix_icon wxi-download"></span></a>`;
}

function storedText(packages, labels) {
    if (!Array.isArray(packages) || packages.length === 0)
        return labels.storedEmpty;

    const first = packages[0];
    const sameVersion = packages.every(item =>
        item.versionName === first.versionName && item.versionCode === first.versionCode);

    if (sameVersion) {
        const head = `Версия ${escapeHtml(first.versionName)}, сборка ${escapeHtml(first.versionCode)}`;
        const files = packages.map(item => `${escapeHtml(item.abi)} — ${escapeHtml(packageFileName(item))}${downloadLink(item)}`);
        return [head, ...files].join("<br>");
    }

    return packages
        .map(item => `${escapeHtml(item.abi)} — версия ${escapeHtml(item.versionName)}, сборка ${escapeHtml(item.versionCode)} — ${escapeHtml(packageFileName(item))}${downloadLink(item)}`)
        .join("<br>");
}

function showStoredText(text) {
    const status = $$(STATUS_ID);
    if (!status)
        return;

    status.define("template", text);
    status.refresh();
}

let storedGeneration = 0;

function showStoredPackages(labels) {
    return readStoredPackages(labels);
}

async function readStoredPackages(labels) {
    const generation = ++storedGeneration;

    try {
        const response = await fetch("/api/tsd/update/stored");
        const current = () => generation === storedGeneration;

        if (!response.ok) {
            if (current())
                showStoredText(labels.storedFailed);
            return null;
        }

        const packages = await response.json();
        if (current())
            showStoredText(storedText(packages, labels));
        return packages;
    } catch {
        if (generation === storedGeneration)
            showStoredText(labels.storedFailed);
        return null;
    }
}

function openWaitDialog() {
    const dialog = webix.ui({
        view: "window",
        modal: true,
        position: "center",
        head: SECTION_TITLE,
        width: 360,
        body: {
            template: "Загрузка пакета…",
            height: 50,
            borderless: true,
        },
    });

    dialog.show();
    return dialog;
}

let downloadInProgress = false;

async function downloadTsdApp(labels) {
    if (downloadInProgress)
        return;

    downloadInProgress = true;
    const dialog = openWaitDialog();

    try {
        const form = $$("tsdAppUpdate.repositoryUrl")?.getFormView();
        if (!form) {
            webix.message({ text: "Не удалось сохранить настройки", type: "error" });
            return;
        }

        const packet = await saveConfiguration(form.config.id);
        if (!packet?.isSuccess)
            return;

        const response = await fetch("/api/tsd/update/refresh", { method: "POST" });

        if (!response.ok) {
            const text = await response.text();
            webix.message({ text: text || "Не удалось получить Fmu-Api-TSD", type: "error" });
            return;
        }

        const payload = await response.json();
        if (Array.isArray(payload.errors) && payload.errors.length > 0) {
            webix.message({ text: payload.errors.join("; "), type: "error" });
            await showStoredPackages(labels);
            return;
        }

        const packages = await showStoredPackages(labels);
        if (!Array.isArray(packages)) {
            webix.message({ text: labels.storedFailed, type: "error" });
            return;
        }

        if (packages.length === 0)
            webix.message({ text: "Пакет не загружен", type: "error" });
    } catch {
        webix.message({ text: "Не удалось получить Fmu-Api-TSD", type: "error" });
    } finally {
        dialog.close();
        downloadInProgress = false;
    }
}

export default function (id, config) {
    return new TsdAppUpdateElement(id)
        .loadConfig(config)
        .render();
}
