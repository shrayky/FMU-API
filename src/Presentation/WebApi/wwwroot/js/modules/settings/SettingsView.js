import { loadParameters, saveConfiguration } from '../../services/ConfigurationService.js';

const SETTINGS_MODULES = {
    serverConfigData: () => import("./elements/serverConfiguration.js"),
    organizationsTable: () => import("./elements/organizationsTable.js"),
    salesControl: () => import("./elements/salesControl.js"),
    minimalPrices: () => import("./elements/minimalPrices.js"),
    pingHosts: () => import("./elements/pingHostsTable.js"),
    databaseConnection: () => import("./elements/databaseConnection.js"),
    connectedFrontol: () => import("./elements/connectedFrontol.js"),
    //markUnit: () => import("./elements/markUnit.js"),
    timeoutConfig: () => import("./elements/timeouts.js"),
    loggingConfigData: () => import("./elements/loggingConfiguration.js"),
    autoUpdateData: () => import("./elements/autoUpdate.js"),
    fmuApiCentralServer: () => import("./elements/centralServerConnection.js"),
};

export default function SettingsView(id) {
    $$("toolbarLabel").setValue("FMU-API: Настройка службы");

    let loadingController = null;
    let isViewVisible = false;

    return {
        id: id,
        view: "form",
        complexData: true,
        hidden: true,
        rows: [
            {
                cols: [
                    {
                        id: "saveConfigurationButton",
                        view: "button",
                        value: "Сохранить",
                        autowidth: false,
                        width: 120,
                        hidden: true,
                        click: function () {
                            saveConfiguration(id);
                        }
                    },
                    {}
                ]
            },
            {
                id: "settingsScroll",
                view: "scrollview",
                scroll: "y",
                body: {
                    rows: [
                    ]
                },
            },
        ],
        on: {
            onViewShow: async function () {
                isViewVisible = true;
                const form = $$(id);
                const scrollview = $$("settingsScroll").getBody();

                webix.extend(form, webix.ProgressBar);

                try {
                    form.showProgress({
                        type: "icon",
                    });
                    loadingController = new AbortController();
                    
                    const config = await loadParameters({ signal: loadingController.signal });

                    if (!isViewVisible) return;

                    for (const moduleId of Object.keys(SETTINGS_MODULES)) {
                        const module = await SETTINGS_MODULES[moduleId]();
                        const elements = module.default(moduleId, config);
                        scrollview.addView(elements);
                    }

                    $$("saveConfigurationButton").show();

                } catch (error) {
                    if (error.name === 'AbortError') {
                        console.log('Загрузка настроек отменена');
                        return;
                    }

                    if (isViewVisible) {
                        webix.message({
                            type: "error",
                            text: "Ошибка загрузки настроек"
                        });
                    }
                    console.error("Ошибка загрузки настроек:", error);
                }
                finally {
                    if (isViewVisible)
                        form.hideProgress();
                }
            },
            onDestruct: function() {
                if (loadingController) {
                    loadingController.abort();
                    loadingController = null;
                }
            },
        }
    };
}

function prepareForm(form) {

}
