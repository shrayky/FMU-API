import { saveConfigurationSection } from "../../../services/ConfigurationService.js";
import { CheckBox, Label, Number, Text, padding } from "../../../utils/ui.js";
import { httpAddressValidation } from "../../../utils/validators.js";

const SECTION_NAME = "tsdAppUpdate";
const VIEW_ID = "tsdAppUpdateSection";
const SETTINGS_ID = "tsdAppUpdateSettings";
const STATUS_ID = "tsdAppUpdateStatus";
const REPOSITORY_URL_NAME = `${SECTION_NAME}.repositoryUrl`;
const ENABLED_NAME = `${SECTION_NAME}.enabled`;
const CHECK_INTERVAL_NAME = `${SECTION_NAME}.checkIntervalMinutes`;

const DEFAULT_REPOSITORY_URL = "https://github.com/shrayky/fmu-api-tsd-app";

const LABELS = {
    updateGroup: "Обновление дистрибутива Fmu-Api-TSD из GitHub",
    packagesGroup: "Файлы дистрибутива",
    enabled: "Использовать",
    repository: "Адрес GitHub",
    checkInterval: "Интервал проверки (минут)",
    checkUpdate: "Проверить обновление",
    upload: "Загрузить",
    storedEmpty: "Пакетов нет",
    storedFailed: "Не удалось прочитать пакеты",
    settingsSaveFailed: "Не удалось сохранить настройки",
    receiveFailed: "Не удалось проверить обновление Fmu-Api-TSD",
    uploadFailed: "Не удалось загрузить Fmu-Api-TSD",
    tooManyFiles: "Можно загрузить не больше двух APK",
    packageNotLoaded: "Пакет не загружен",
    loadingPackage: "Загрузка пакета…",
};

/**
 * Блок настроек Fmu-Api-TSD: обновление дистрибутива из GitHub и файлы APK,
 * которые нода уже сохранила. Выводится на странице Fmu-Api-TSD.
 */
export class TsdAppUpdateSection {
    constructor(config) {
        const settings = config?.[SECTION_NAME];

        this.enabled = settings?.enabled ?? false;
        this.checkIntervalMinutes = settings?.checkIntervalMinutes ?? 120;
        this.repositoryUrl = settings?.repositoryUrl || DEFAULT_REPOSITORY_URL;
    }

    render() {
        webix.delay(() => showStoredPackages());

        return {
            id: VIEW_ID,
            rows: [
                Label("lTsdUpdateGroup", LABELS.updateGroup),
                {
                    padding: padding,
                    rows: [
                        CheckBox(LABELS.enabled, ENABLED_NAME, {
                            value: this.enabled,
                            on: {
                                onChange: (enabled) => setSettingsEnabled(enabled),
                            },
                        }),

                        {
                            id: SETTINGS_ID,
                            disabled: !this.enabled,
                            rows: [
                                Number(LABELS.checkInterval, CHECK_INTERVAL_NAME, this.checkIntervalMinutes),

                                Text(LABELS.repository, REPOSITORY_URL_NAME, this.repositoryUrl, {
                                    ...httpAddressValidation,
                                    placeholder: "https://github.com/owner/repo",
                                }),

                                {
                                    cols: [
                                        {
                                            view: "button",
                                            value: LABELS.checkUpdate,
                                            autowidth: false,
                                            width: 200,
                                            click: () => downloadPackage(),
                                        },
                                        {}
                                    ],
                                },
                            ],
                        },
                    ],
                },
                Label("lTsdPackagesGroup", LABELS.packagesGroup),
                {
                    padding: padding,
                    rows: [
                        {
                            view: "template",
                            id: STATUS_ID,
                            autoheight: true,
                            borderless: true,
                            css: { "line-height": "20px" },
                            template: LABELS.storedEmpty,
                        },

                        {
                            margin: 8,
                            cols: [
                                {
                                    view: "button",
                                    value: LABELS.upload,
                                    autowidth: false,
                                    width: 140,
                                    click: () => uploadPackage(),
                                },
                                {}
                            ],
                        },

                    ],
                },
            ],
        };
    }
}

function setSettingsEnabled(enabled) {
    const settings = $$(SETTINGS_ID);

    if (!settings)
        return;

    if (enabled)
        settings.enable();
    else
        settings.disable();
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

function packageLink(item, text) {
    const href = "/api/tsd/update/apk?abi=" + encodeURIComponent(item.abi);
    const name = escapeHtml(packageFileName(item));
    return `<a href="${href}" download="${name}" title="Скачать ${name}" style="color:#3d8fe0;text-decoration:none">${text}</a>`;
}

function storedText(packages) {
    if (!Array.isArray(packages) || packages.length === 0)
        return LABELS.storedEmpty;

    const first = packages[0];
    const sameVersion = packages.every(item =>
        item.versionName === first.versionName && item.versionCode === first.versionCode);

    if (sameVersion) {
        const head = `Версия ${escapeHtml(first.versionName)}, сборка ${escapeHtml(first.versionCode)}`;
        const files = packages.map(item => packageLink(item,
            `${escapeHtml(item.abi)} — ${escapeHtml(packageFileName(item))}`));
        return [head, ...files].join("<br>");
    }

    return packages
        .map(item => packageLink(item,
            `${escapeHtml(item.abi)} — версия ${escapeHtml(item.versionName)}, сборка ${escapeHtml(item.versionCode)} — ${escapeHtml(packageFileName(item))}`))
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

function showStoredPackages() {
    return readStoredPackages();
}

async function readStoredPackages() {
    const generation = ++storedGeneration;

    try {
        const response = await fetch("/api/tsd/update/stored");

        if (!response.ok) {
            if (generation === storedGeneration)
                showStoredText(LABELS.storedFailed);
            return null;
        }

        const packages = await response.json();

        if (generation === storedGeneration)
            showStoredText(storedText(packages));

        return packages;
    } catch {
        if (generation === storedGeneration)
            showStoredText(LABELS.storedFailed);
        return null;
    }
}

function openWaitDialog() {
    const dialog = webix.ui({
        view: "window",
        modal: true,
        position: "center",
        head: "Fmu-Api-TSD",
        width: 360,
        body: {
            template: LABELS.loadingPackage,
            height: 50,
            borderless: true,
        },
    });

    dialog.show();
    return dialog;
}

let downloadInProgress = false;

async function downloadPackage() {
    if (downloadInProgress)
        return;

    const form = $$(REPOSITORY_URL_NAME)?.getFormView();
    if (!form) {
        webix.message({ text: LABELS.settingsSaveFailed, type: "error" });
        return;
    }

    if (!form.validate())
        return;

    downloadInProgress = true;
    const dialog = openWaitDialog();

    try {
        const packet = await saveConfigurationSection(form.config.id, SECTION_NAME);
        if (!packet?.isSuccess)
            return;

        const response = await fetch("/api/tsd/update/refresh", { method: "POST" });
        await finishPackageResponse(response, LABELS.receiveFailed);
    } catch {
        webix.message({ text: LABELS.receiveFailed, type: "error" });
    } finally {
        dialog.close();
        downloadInProgress = false;
    }
}

function uploadPackage() {
    if (downloadInProgress)
        return;

    const input = document.createElement("input");
    input.type = "file";
    input.accept = ".apk,application/vnd.android.package-archive";
    input.multiple = true;
    input.onchange = () => {
        const files = input.files;
        if (!files || files.length === 0)
            return;

        sendApkFiles(files);
    };
    input.click();
}

async function sendApkFiles(fileList) {
    if (downloadInProgress)
        return;

    const files = Array.from(fileList);
    if (files.length > 2) {
        webix.message({ text: LABELS.tooManyFiles, type: "error" });
        return;
    }

    downloadInProgress = true;
    const dialog = openWaitDialog();

    try {
        const body = new FormData();
        for (const file of files)
            body.append("files", file, file.name);

        const response = await fetch("/api/tsd/update/apk", { method: "POST", body });
        await finishPackageResponse(response, LABELS.uploadFailed);
    } catch {
        webix.message({ text: LABELS.uploadFailed, type: "error" });
    } finally {
        dialog.close();
        downloadInProgress = false;
    }
}

async function finishPackageResponse(response, failureText) {
    if (!response.ok) {
        const text = await response.text();
        webix.message({ text: text || failureText, type: "error" });
        return;
    }

    const payload = await response.json();
    if (Array.isArray(payload.errors) && payload.errors.length > 0) {
        webix.message({ text: payload.errors.join("; "), type: "error" });
        await showStoredPackages();
        return;
    }

    const packages = await showStoredPackages();
    if (!Array.isArray(packages)) {
        webix.message({ text: LABELS.storedFailed, type: "error" });
        return;
    }

    if (packages.length === 0)
        webix.message({ text: LABELS.packageNotLoaded, type: "error" });
}
