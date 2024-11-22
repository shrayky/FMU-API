import { Label, Text, padding } from "../../../utils/ui.js";
import { httpAddressValidation } from "../../../utils/validators.js";

class TokenServiceConfigurationElement {
    constructor(id) {
        this.id = id;
        this.SETTINGS_ID = "tokenService";
        this.LABELS = {
            title: "Сервис получения токена для авторизации в Честном знаке",
            addres: "Адрес",
        };
    }

    loadConfig(config) {
        if (config?.trueSignTokenService) {
            this.addres = config.trueSignTokenService.connectionAddres;
        }

        return this;
    }

    render() {
        var elements = [];

        elements.push(
            Label("lTruesignService", this.LABELS.title),
        );

        elements.push(
            {
                padding: padding,
                rows: [
                    Text("Адрес", "trueSignTokenService.connectionAddres", this.addres, httpAddressValidation)
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