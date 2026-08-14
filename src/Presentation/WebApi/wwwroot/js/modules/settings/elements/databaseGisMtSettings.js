import databaseConnection from "./databaseConnection.js";
import statisticsSettings from "./statisticsSettings.js";
import gisMtSettings from "./gisMtSettings.js";

const TABVIEW_ID = "databaseGisMtTabView";
const DATABASE_TAB_ID = "databaseConnectionTab";
const STATISTICS_TAB_ID = "statisticsSettingsTab";
const GIS_MT_TAB_ID = "gisMtSettingsTab";

const DATABASE_DEPENDENT_TAB_IDS = [STATISTICS_TAB_ID, GIS_MT_TAB_ID];

function setDatabaseDependentTabsEnabled(enabled) {
    const tabview = $$(TABVIEW_ID);
    if (!tabview)
        return;

    const tabbar = tabview.getTabbar();
    if (enabled) {
        DATABASE_DEPENDENT_TAB_IDS.forEach((tabId) => tabbar.enableOption(tabId));
        return;
    }

    if (DATABASE_DEPENDENT_TAB_IDS.includes(tabbar.getValue()))
        tabbar.setValue(DATABASE_TAB_ID);

    DATABASE_DEPENDENT_TAB_IDS.forEach((tabId) => tabbar.disableOption(tabId));
}

function bindDatabaseEnableToDependentTabs() {
    const checkbox = $$("database.enable");

    if (!checkbox)
        return;

    checkbox.attachEvent("onChange", (enabled) => {
        setDatabaseDependentTabsEnabled(Boolean(enabled));
    });

    setDatabaseDependentTabsEnabled(Boolean(checkbox.getValue()));
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
                        header: "Статистика",
                        body: statisticsSettings(STATISTICS_TAB_ID, config)
                    },
                    {
                        header: "ГИС МТ",
                        body: gisMtSettings(GIS_MT_TAB_ID, config)
                    }
                ]
            }
        ]
    };

    setTimeout(bindDatabaseEnableToDependentTabs, 500);

    return view;
}
