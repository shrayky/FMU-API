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
            responseEncoding: "Кодировка ответа проверки марки",
        };

        this.LOCAL_MODULE_VERSIONS = [
            { id: 0, value: "Не указана (по умолчанию используется версия 1)" },
            { id: 1, value: "Версия 1" },
            { id: 2, value: "Версия 2" },
        ];

        this.RESPONSE_ENCODINGS = [
            { id: 0, value: "UTF-8 (Frontol и др.)" },
            { id: 1, value: "Windows-1251 (1С)" },
        ];
    }

    loadConfig(config) {
        if (config?.serverConfig) {
            this.apiIpPort = config.serverConfig.apiIpPort ?? 2578;
            this.tsPiotEnabled = config.serverConfig.tsPiotEnabled ?? false;
            this.localModuleVersion = config.serverConfig.localModuleVersion ?? 0;
            this.responseEncoding = config.serverConfig.responseEncoding ?? 0;
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
                                labelWidth: 250,
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
                    },

                    {
                        view: "select",
                        label: this.LABELS.responseEncoding,
                        labelPosition: "left",
                        labelWidth: 250,
                        name: "serverConfig.responseEncoding",
                        options: this.RESPONSE_ENCODINGS,
                        value: this.responseEncoding,
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