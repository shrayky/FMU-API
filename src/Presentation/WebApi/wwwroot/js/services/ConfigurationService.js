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

const INT32_MAX = 2147483647;

/// Приводит значение к Int32 в диапазоне min…Int32. Иначе возвращает fallback.
function toInt32(value, fallback, min = 0) {
    const id = parseInt(value, 10);
    if (!Number.isInteger(id) || id < min || id > INT32_MAX)
        return fallback;
    return id;
}

function normalizeFormValues(values) {
    const frontol = values?.connectedFrontolSettings;
    if (!frontol)
        return values;

    if (Array.isArray(frontol.connectionSettings)) {
        frontol.connectionSettings = frontol.connectionSettings.map((item, index) => ({
            ...item,
            id: toInt32(item.id, index + 1, 1)
        }));
    }

    frontol.printGroupSourseId = toInt32(frontol.printGroupSourseId, 0);

    const serverConfig = values?.serverConfig;
    if (serverConfig) {
        serverConfig.localModuleVersion = parseInt(serverConfig.localModuleVersion, 10) || 0;
        serverConfig.responseEncoding = parseInt(serverConfig.responseEncoding, 10) || 0;
    }

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

    saveParameters(data)
        .then(packet => {
            if (!packet.isSuccess) {
                webix.message(packet);
                return;
            }

            if (packet.needToRestart) {
                webix.message("Изменения будут применены после перезапуска службы DS:Fmu-Api (она произойдет в течение одной минуты)");
            }

            window.dispatchEvent(new CustomEvent(SETTINGS_SAVED_EVENT, {
                detail: values
            }));
        })
        .finally(() => {
            form.enable();
        });
}
