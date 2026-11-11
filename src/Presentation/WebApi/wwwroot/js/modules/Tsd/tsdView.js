import { loadParameters, saveConfigurationSection } from "../../services/ConfigurationService.js";
import { TsdAppUpdateSection } from "../settings/elements/tsdAppUpdate.js";

const TITLE = "Fmu-Api-TSD";

const SECTION_NAME = "tsdAppUpdate";
const SCROLL_ID = "tsdSettingsScroll";
const SAVE_BUTTON_ID = "saveTsdAppUpdateButton";

const LABELS = {
    toolbar: `FMU-API: ${TITLE}`,
    save: "Сохранить",
    saved: "Настройки сохранены",
    loadFailed: "Ошибка загрузки настроек",
    saveFailed: "Не удалось сохранить настройки",
};

/**
 * Страница Fmu-Api-TSD: настройки получения APK терминала сбора данных
 * и список пакетов, которые нода уже отдаёт ТСД.
 */
export default function tsdView(id) {
    $$("toolbarLabel").setValue(LABELS.toolbar);

    let loadingController = null;
    let isViewVisible = false;

    return {
        id: id,
        view: "form",
        complexData: true,
        hidden: true,
        rows: [
            {
                margin: 8,
                cols: [
                    {
                        id: SAVE_BUTTON_ID,
                        view: "button",
                        value: LABELS.save,
                        autowidth: false,
                        width: 120,
                        hidden: true,
                        click: () => saveSection(id),
                    },
                    {},
                ],
            },
            {
                id: SCROLL_ID,
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
                const scrollview = $$(SCROLL_ID).getBody();

                webix.extend(form, webix.ProgressBar);

                try {
                    form.showProgress({
                        type: "icon",
                    });
                    loadingController = new AbortController();

                    const config = await loadParameters({ signal: loadingController.signal });

                    if (!isViewVisible) return;

                    const existingViews = Array.from(scrollview.getChildViews());
                    existingViews.forEach(view => scrollview.removeView(view.config.id));

                    scrollview.addView(new TsdAppUpdateSection(config).render());

                    $$(SAVE_BUTTON_ID).show();
                } catch (error) {
                    if (error.name === 'AbortError') {
                        console.log('Загрузка настроек ТСД отменена');
                        return;
                    }

                    if (isViewVisible) {
                        webix.message({
                            type: "error",
                            text: LABELS.loadFailed
                        });
                    }
                    console.error("Ошибка загрузки настроек ТСД:", error);
                }
                finally {
                    if (isViewVisible)
                        form.hideProgress();
                }
            },
            onDestruct: function () {
                isViewVisible = false;

                if (loadingController) {
                    loadingController.abort();
                    loadingController = null;
                }
            },
        }
    };
}

async function saveSection(formId) {
    if (!$$(formId)?.validate())
        return;

    try {
        const packet = await saveConfigurationSection(formId, SECTION_NAME);

        if (packet?.isSuccess)
            webix.message({ type: "success", text: LABELS.saved });
    } catch {
        webix.message({ text: LABELS.saveFailed, type: "error" });
    }
}
