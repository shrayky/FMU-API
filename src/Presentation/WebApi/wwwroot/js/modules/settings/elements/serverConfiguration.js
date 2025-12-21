import {Label, Text, Number, padding, CheckBox} from "../../../utils/ui.js";

class ServerConfigurationElement {
    constructor(id) {
        this.id = id;
        this.LABELS = {
            title: "Сервер",
            name: "Название узла",
            ipPortApi: "IP-порт API сервиса",
            tsPiotUse: "Для проверки маркировки использовать ТС ПиОТ",
        };
    }

    loadConfig(config) {
        if (config?.serverConfig) {
            this.apiIpPort = config.serverConfig.apiIpPort ?? 2578;
            this.tsPiotEnabled = config.serverConfig.tsPiotEnabled ?? false;
        }

        if (config?.nodeName) {
            this.nodeName = config.nodeName
        }

        return this;
    }

    render() {
        var elements = [];

        elements.push(
            Label("lServerConfig", this.LABELS.title),
        );

        elements.push(
            {
                padding: padding,
                rows: [
                    Text("Название узла", "nodeName", this.nodeName),
                    Number("IP-порт API сервиса", "serverConfig.apiIpPort", this.apiIpPort, "111"),
                    CheckBox(this.LABELS.tsPiotUse, "serverConfig.tsPiotEnabled", {
                        value: this.tsPiotEnabled,
                        Label: this.LABELS.tsPiotUse
                    })
                ]
            }
        );

        return { id: this.id, rows: elements };
    }
}

export default function (id, config) {
    return new ServerConfigurationElement(id)
        .loadConfig(config)
        .render();
}