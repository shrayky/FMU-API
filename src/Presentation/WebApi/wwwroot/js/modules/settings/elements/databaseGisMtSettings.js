import databaseConnection from "./databaseConnection.js";
import gisMtSettings from "./gisMtSettings.js";

const TABVIEW_ID = "databaseGisMtTabView";
const DATABASE_TAB_ID = "databaseConnectionTab";
const GIS_MT_TAB_ID = "gisMtSettingsTab";

function setGisMtTabEnabled(enabled) {
    const tabview = $$(TABVIEW_ID);
    if (!tabview)
        return;

    const tabbar = tabview.getTabbar();
    if (enabled) {
        tabbar.enableOption(GIS_MT_TAB_ID);
        return;
    }

    if (tabbar.getValue() === GIS_MT_TAB_ID)
        tabbar.setValue(DATABASE_TAB_ID);

    tabbar.disableOption(GIS_MT_TAB_ID);
}

function bindDatabaseEnableToGisMtTab() {
    const checkbox = $$("database.enable");

    if (!checkbox)
        return;

    checkbox.attachEvent("onChange", (enabled) => {
        setGisMtTabEnabled(Boolean(enabled));
    });

    setGisMtTabEnabled(Boolean(checkbox.getValue()));
}

export default function (id, config) {
    const view = {
        id,
        rows: [
            {
                view: "tabview",
                id: TABVIEW_ID,
                tabbar: {
                    optionWidth: 180
                },
                multiview: {
                    keepViews: true
                },
                cells: [
                    {
                        header: "База данных",
                        body: databaseConnection(DATABASE_TAB_ID, config)
                    },
                    {
                        header: "ГИС МТ",
                        body: gisMtSettings(GIS_MT_TAB_ID, config)
                    }
                ]
            }
        ]
    };

    setTimeout(bindDatabaseEnableToGisMtTab, 500);

    return view;
}
