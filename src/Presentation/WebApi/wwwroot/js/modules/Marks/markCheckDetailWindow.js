/**
 * Окно с последним запросом Frontol и ответом FMU по марке.
 */
export function openMarkCheckDetailWindow({ sgtin, check }) {
    const windowId = "MarkCheckDetailWindow";
    if ($$(windowId))
        $$(windowId).destructor();

    const requestJson = formatJson(check?.checkRequest);
    const responseJson = formatJson(check?.checkResponse);
    const checkDate = check?.checkDate
        ? new Date(check.checkDate).toLocaleString()
        : "";

    webix.ui({
        view: "window",
        id: windowId,
        position: "center",
        modal: true,
        width: Math.min(window.innerWidth * 0.9, 1100),
        height: Math.min(window.innerHeight * 0.85, 700),
        resize: true,
        head: {
            view: "toolbar",
            elements: [
                {
                    view: "label",
                    label: `Проверка марки ${sgtin}${checkDate ? ` — ${checkDate}` : ""}`
                },
                {
                    view: "icon",
                    icon: "wxi-close",
                    click: () => $$(windowId).close()
                }
            ]
        },
        body: {
            cols: [
                {
                    rows: [
                        {
                            view: "label",
                            label: "Запрос",
                            align: "center"
                        },
                        {
                            view: "textarea",
                            readonly: true,
                            value: requestJson
                        }
                    ]
                },
                { view: "resizer" },
                {
                    rows: [
                        {
                            view: "label",
                            label: "Ответ",
                            align: "center"
                        },
                        {
                            view: "textarea",
                            readonly: true,
                            value: responseJson
                        }
                    ]
                }
            ]
        }
    }).show();
}

/**
 * Форматирует объект в JSON для отображения в окне.
 */
function formatJson(value) {
    if (!value)
        return "Нет данных";

    return JSON.stringify(value, null, 2);
}
