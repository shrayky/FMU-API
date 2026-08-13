import centralServerConnection from "./centralServerConnection.js";
import autoUpdate from "./autoUpdate.js";

const TABVIEW_ID = "centralServerAutoUpdateTabView";
const CENTRAL_SERVER_TAB_ID = "centralServerConnectionTab";
const AUTO_UPDATE_TAB_ID = "autoUpdateTab";

export default function (id, config) {
    return {
        id,
        rows: [
            {
                view: "tabview",
                id: TABVIEW_ID,
                tabbar: {
                    optionWidth: 200
                },
                multiview: {
                    keepViews: true
                },
                cells: [
                    {
                        header: "FMU-API-Central",
                        body: centralServerConnection(CENTRAL_SERVER_TAB_ID, config)
                    },
                    {
                        header: "Автообновление",
                        body: autoUpdate(AUTO_UPDATE_TAB_ID, config)
                    }
                ]
            }
        ]
    };
}
