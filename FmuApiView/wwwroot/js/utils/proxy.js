export function InitProxy() {
    var baseAdres = '';

    var elementApiAdres = document.getElementById("ApiServerAdres");

    if (elementApiAdres != null)
        baseAdres = elementApiAdres.value;

    var ajax = webix.ajax();

    webix.proxy.api = {
        $proxy: true,
        init: _ => {
        },

        load: (view, params) => {
            if (baseAdres == '') {
                webix.message("Служба не настроена - не указан порт сервера api. Настройте и перезапустите ее.");
                return []
            }

            view.disable();

            var url = view.config.url.source;

            return ajax.get(`${baseAdres}${url}`)
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
            if (baseAdres == '') {
                webix.message("Служба не настроена - не указан порт сервера api. Настройте и перезапустите ее.");
                return []
            }

            view.disable();

            var url = view.config.url.source;
        },

    }
}