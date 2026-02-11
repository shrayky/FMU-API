import {Label, Text, Number, padding, CheckBox} from "../../../utils/ui.js";

class ServerConfigurationElement {
    constructor(id) {
        this.id = id;
        this.LABELS = {
            title: "Сервер",
            name: "Название узла",
            ipPortApi: "IP-порт API сервиса",
            tsPiotUse: "Для проверки маркировки использовать ТС ПиОТ",
            localModuleVersion: "Версия локального модуля",
        };

        this.LOCAL_MODULE_VERSIONS = [
            { value: 0, label: "Не указана (по умолчанию используется версия 1)" },
            { value: 1, label: "Версия 1" },
            { value: 2, label: "Версия 2" },
        ];
    }

    loadConfig(config) {
        if (config?.serverConfig) {
            this.apiIpPort = config.serverConfig.apiIpPort ?? 2578;
            this.tsPiotEnabled = config.serverConfig.tsPiotEnabled ?? false;
            this.localModuleVersion = config.serverConfig.localModuleVersion ?? 0;
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
                    }),

                    {
                        cols: [
                            {
                                view: "select",
                                label: this.LABELS.localModuleVersion,
                                labelPosition: "left",
                                labelWidth: 200,
                                id: "localModuleVersion",
                                name: "serverConfig.localModuleVersion",
                                options: this.LOCAL_MODULE_VERSIONS,
                                value: this.localModuleVersion,
                            },
                            {
                                view: "template",
                                id: "localModuleVersionTemplate",
                                template: "если 0 - по умолчанию используется версия 1",
                                borderless: true,
                                css: {
                                    "font-style": "italic",
                                    "padding": "4px"
                                }
                            }        
                        ]
                    }
                    
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