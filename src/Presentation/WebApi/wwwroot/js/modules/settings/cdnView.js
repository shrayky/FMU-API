import { Label } from "../../utils/ui.js";
import { ApiServerAdres } from '../../utils/net.js';

export default function cdnView(id) {
    $$("toolbarLabel").setValue("FMU-API: Список cdn-серверов");

    return {
        view: "datatable",
        id: id,
        columns: [
            {
                id: "host",
                header: "Хост",
                fillspace: true,
            },

            {
                id: "latency",
                header: "Задержка"
            },

            {
                id: "offline",
                header: "Оффлайн"
            },
        ],

        scroll: false,

        on: {
            onBeforeLoad: function () {
                this.showOverlay("Загружаю...")
            },

            onAfterLoad: function () {
                this.hideOverlay();
            }
        },

        url: function (params) {
            return webix.ajax(ApiServerAdres("/configuration/cdn"));
        },
    }
    
}