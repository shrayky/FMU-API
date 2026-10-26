const PARAMETERS_URL = "api/configuration/parameters";

export const SETTINGS_SAVED_EVENT = "settingsSaved";

export async function loadParameters({ signal } = {}) {
    const options = signal ? { signal } : null;
    const response = await webix.ajax().get(PARAMETERS_URL, options);
    return response.json().content;
}

export function saveParameters(data) {
    return webix.ajax()
        .post(PARAMETERS_URL, data)
        .then(answer => answer.json());
}

export async function exportConfiguration() {
    const response = await fetch(`${PARAMETERS_URL}/exchange`);
    if (!response.ok) {
        webix.message({ type: "error", text: "Не удалось экспортировать настройки" });
        return;
    }

    const blob = await response.blob();
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = "config.json";
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
}

export function importConfiguration(formId) {
    const input = document.createElement("input");
    input.type = "file";
    input.accept = ".json,application/json";
    input.onchange = () => {
        const file = input.files && input.files[0];
        if (!file)
            return;

        const reader = new FileReader();
        reader.onload = () => uploadImportedSettings(formId, reader.result);
        reader.readAsText(file);
    };
    input.click();
}

function uploadImportedSettings(formId, json) {
    const form = $$(formId);
    form.disable();

    fetch(`${PARAMETERS_URL}/exchange`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: json
    })
        .then(async response => {
            if (!response.ok) {
                const text = await response.text();
                webix.message({ type: "error", text: text || "Не удалось импортировать настройки" });
                return;
            }

            const packet = await response.json();
            if (packet.needToRestart) {
                webix.message("Изменения будут применены после перезапуска службы DS:Fmu-Api (она произойдет в течение одной минуты)");
            } else {
                webix.message("Настройки импортированы");
            }

            const config = await loadParameters();
            window.dispatchEvent(new CustomEvent(SETTINGS_SAVED_EVENT, {
                detail: config
            }));
            form.callEvent("onViewShow");
        })
        .catch(() => {
            webix.message({ type: "error", text: "Не удалось импортировать настройки" });
        })
        .finally(() => {
            form.enable();
        });
}

const INT32_MAX = 2147483647;

/// Приводит значение к Int32 в диапазоне min…Int32. Иначе возвращает fallback.
function toInt32(value, fallback, min = 0) {
    const id = parseInt(value, 10);
    if (!Number.isInteger(id) || id < min || id > INT32_MAX)
        return fallback;
    return id;
}

function normalizeFormValues(values) {
    const serverConfig = values?.serverConfig;
    if (serverConfig) {
        if (serverConfig.localModuleGeneral) {
            serverConfig.localModuleGeneral.version = parseInt(serverConfig.localModuleGeneral.version, 10) || 0;
        }
        serverConfig.responseEncoding = parseInt(serverConfig.responseEncoding, 10) || 0;
    }

    const frontol = values?.connectedFrontolSettings;
    if (!frontol)
        return values;

    if (Array.isArray(frontol.connectionSettings)) {
        frontol.connectionSettings = frontol.connectionSettings.map((item, index) => ({
            ...item,
            id: toInt32(item.id, index + 1, 1)
        }));
    }

    frontol.frontolWareDataSourceId = toInt32(
        frontol.frontolWareDataSourceId ?? frontol.printGroupSourseId,
        0
    );
    delete frontol.printGroupSourseId;

    if (frontol.syncBeerTapsSettings) {
        frontol.syncBeerTapsSettings.syncBeerTapsPeriodSeconds =
            parseInt(frontol.syncBeerTapsSettings.syncBeerTapsPeriodSeconds, 10) || 30;
    }

    const gisMtSettings = values?.gisMtSettings;
    if (gisMtSettings?.stockLoadTime instanceof Date) {
        const date = gisMtSettings.stockLoadTime;
        const hours = date.getHours().toString().padStart(2, "0");
        const minutes = date.getMinutes().toString().padStart(2, "0");
        const seconds = date.getSeconds().toString().padStart(2, "0");
        gisMtSettings.stockLoadTime = `${hours}:${minutes}:${seconds}`;
    }

    return values;
}

export function saveConfiguration(formId) {
    const form = $$(formId);
    const values = normalizeFormValues(form.getValues());
    const data = JSON.stringify(values);

    form.disable();

    return saveParameters(data)
        .then(packet => {
            if (!packet.isSuccess) {
                webix.message(packet);
                return packet;
            }

            if (packet.needToRestart) {
                webix.message("Изменения будут применены после перезапуска службы DS:Fmu-Api (она произойдет в течение одной минуты)");
            }

            window.dispatchEvent(new CustomEvent(SETTINGS_SAVED_EVENT, {
                detail: values
            }));

            return packet;
        })
        .finally(() => {
            form.enable();
        });
}
