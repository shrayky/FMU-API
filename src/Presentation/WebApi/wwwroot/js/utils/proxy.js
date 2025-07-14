export function InitProxy() {
    let baseAddress = '';
    let apiIpPort = 2589;

    const elementApiServerIpPort = document.getElementById("ApiServerIpPort"); 

    if (elementApiServerIpPort != null)
        apiIpPort = elementApiServerIpPort.value;

    baseAddress = `http://${window.location.hostname}:${apiIpPort}/api`;

    var ajax = webix.ajax();

    webix.proxy.api = {
        $proxy: true,
        init: _ => {
        },

        load: (view, params) => {
            if (baseAddress == '') {
                webix.message("Служба не настроена - не указан порт сервера api. Настройте и перезапустите ее.");
                return []
            }

            view.disable();

            var url = view.config.url.source;

            return ajax.get(`${baseAddress}${url}`)
                .then(answer => {
                    var packet = answer.json();

                    if (packet.isSuccess != undefined) {
                        if (!packet.isSuccess)
                            webix.message(packet.message);
                    }

                    view.enable();

                    return packet.content;

                },
                    err => {
                        view.enable();
                        webix.message(err);
                    }
                );
        },

        save: (view, params) => {
            if (baseAddress == '') {
                webix.message("Служба не настроена - не указан порт сервера api. Настройте и перезапустите ее.");
                return []
            }

            view.disable();

            var url = view.config.url.source;
        },

    }
}