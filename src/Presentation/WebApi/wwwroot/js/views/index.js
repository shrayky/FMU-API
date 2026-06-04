// js/views/index.js
import '../utils/customComponents.js';
import { InitProxy } from '../utils/proxy.js';
import { RouterService } from '../services/RouterService.js';
import { loadParameters, SETTINGS_SAVED_EVENT } from '../services/ConfigurationService.js';
import { createLayout, createToolbar, createSidebar } from '../components/Layout.js';
import { buildMenuItems } from '../config/menu.js';

import SettingsView from '../modules/settings/SettingsView.js';
import InformationView from '../modules/Information/informationView.js';
import CdnView from '../modules/cdn/cdnView.js';
import LogsView from '../modules/LogsPage/logsView.js';
import MonitorView from '../modules/Monitoring/monitorView.js';
import MarksView from '../modules/Marks/marksView.js';
import MarkCheckView from '../modules/MarkCheck/markCheckView.js';

class App {
    constructor() {
        this.router = new RouterService();
        this.bodyId = "body";
        this.config = null;
        this.initRoutes();
    }

    initRoutes() {
        this.router.register("config", () => SettingsView);
        this.router.register("information", () => InformationView);
        this.router.register("cdnListInfo", () => CdnView);
        this.router.register("logsView", () => LogsView);
        this.router.register("monitorView", () => MonitorView);
        this.router.register("marksView", () => MarksView);
        this.router.register("markCheckView", () => MarkCheckView);
        this.router.register("beerTapsView", async () => (await import("../modules/BeerTaps/beerTapsView.js")).default);
    }

    createMainLayout(config) {
        return createLayout({
            rows: [
                createToolbar("FMU-API"),
                {
                    cols: [
                        createSidebar(
                            buildMenuItems(config),
                            (id) => this.router.navigate(id, this.bodyId)
                        ),
                        { id: this.bodyId }
                    ]
                }
            ]
        });
    }

    refreshMenu(config) {
        this.config = config;

        const items = buildMenuItems(config);
        const sidebar = $$("sidebar");

        if (sidebar) {
            sidebar.clearAll();
            sidebar.parse(items);
        }

        const visibleIds = items.map(item => item.id);
        if (!visibleIds.includes(this.router.currentPage)) {
            this.router.navigate("monitorView", this.bodyId);
        }
    }

    init() {
        InitProxy();

        webix.ready(async () => {
            try {
                this.config = await loadParameters();
            } catch (error) {
                console.error("Ошибка загрузки настроек:", error);
                webix.message({
                    type: "error",
                    text: "Не удалось загрузить настройки, меню отображается по умолчанию"
                });
            }

            webix.ui(this.createMainLayout(this.config));
            this.router.navigate("monitorView", this.bodyId);

            window.addEventListener(SETTINGS_SAVED_EVENT, (event) => {
                this.refreshMenu(event.detail);
            });

            webix.event(window, "resize", () => {
                const root = $$("root");
                root.$setSize(window.innerWidth, window.innerHeight);
            });
        });
    }
}

const app = new App();
app.init();
