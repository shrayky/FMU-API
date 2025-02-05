import { TextBox, Label, padding, CheckBox } from "../../../utils/ui.js";
import { httpAddressValidation } from "../../../utils/validators.js";

const FORM_NAME = "centralServerConnectionSettings";
const SETTINGS_ID = "serverSettings";
const LABELS = {
    title: "Настройка подключения к центральному серверу",
    enabled: "Использовать",
    address: "Веб-адрес сервиса",
    token: "Токен",
    interval: "Интервал обмена (секунд)"
};

export function centralServerConnectionElement(id) {
    var elements = [];

    elements.push(
        Label("lCentralServerConnection", "Настройка подключения к центральному серверу")
    );  

    elements.push(
        {
            view: "subform",
            name: FORM_NAME,
            padding: padding,
            elements: [
                CheckBox("Иcпользовать", "enabled", {
                    on: {
                        onChange: function(enabled) {
                            if (enabled) {
                                $$(SETTINGS_ID).enable();
                            }
                            else {
                                $$(SETTINGS_ID).disable();
                            }
                        }
                    }
                }),
                {
                    id: SETTINGS_ID,
                    disabled: true,
                    rows: [
                        TextBox("text", LABELS.address, "adres", httpAddressValidation),
                        TextBox("text", LABELS.token, "token"),
                        TextBox("number", LABELS.interval, "exchangeRequestInterval")
                    ],
                }
            ]
        }
    );

    return { id: id, rows: elements }
}   
