import { Label, Text, Number, padding } from "../../../utils/ui.js";

class ServerConfigurationElement {
    constructor(id) {
        this.id = id;
        this.LABELS = {
            title: "Сервер",
            name: "Название узла",
            ipPortApi: "IP-порт API сервиса",
        };
    }

    loadConfig(config) {
        if (config?.serverConfig) {
            this.apiIpPort = config.serverConfig.apiIpPort ?? 2578;
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