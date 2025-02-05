import { Label, Text, CheckBox, padding } from "../../../utils/ui.js";
import { httpAddressValidation } from "../../../utils/validators.js";

class TokenServiceConfigurationElement {
    constructor(id) {
        this.id = id;
        this.SETTINGS_ID = "tokenService";
        this.LABELS = {
            title: "Сервис получения токена для авторизации в Честном знаке",
            address: "Адрес",
            enable: "Использовать",
        };
    }

    loadConfig(config) {
        if (config?.trueSignTokenService) {
            this.address = config.trueSignTokenService.connectionAddress;
            this.enable = config.trueSignTokenService.enable;
        }

        return this;
    }

    render() {
        var elements = [];

        elements.push(
            Label("lTrueSignService", this.LABELS.title),
        );

        elements.push(
            {
                padding: padding,
                rows: [
                    CheckBox(this.LABELS.enable, "trueSignTokenService.enable", {
                        value: this.enable,
                        on: {
                            onChange: (enabled) => {
                                if (enabled) {
                                    $$(this.SETTINGS_ID).enable();
                                } else {
                                    $$(this.SETTINGS_ID).disable();
                                }
                            }
                        }                        
                    }),

                    {
                        id: this.SETTINGS_ID,
                        disabled: !this.enable,
                        rows: [
                            Text("Адрес", "trueSignTokenService.connectionAddress", this.address, httpAddressValidation),
                        ]
                    },
                ]
            }
        );

        return { id: this.id, rows: elements };
    }
}

export default function (id, config) {
    return new TokenServiceConfigurationElement(id)
        .loadConfig(config)
        .render();
}