// js/views/index.js
import "../utils/customComponents.js";
import { InitProxy } from '../utils/proxy.js';
import { RouterService } from '../services/RouterService.js';
import { createLayout, createToolbar, createSidebar } from '../components/Layout.js';
import { MENU_ITEMS } from '../config/menu.js';

import SettingsView from '../modules/settings/SettingsView.js';
import InformationView from '../modules/settings/informationView.js';
import CdnView from '../modules/settings/cdnView.js';
import LogsView from '../modules/settings/logsView.js';

class App {
    constructor() {
        this.router = new RouterService();
        this.bodyId = "body";
        this.initRoutes();
    }

    initRoutes() {
        this.router.register("config", () => SettingsView);
        this.router.register("information", () => InformationView);
        this.router.register("cdnListInfo", () => CdnView);
        this.router.register("logsView", () => LogsView);
    }

    createMainLayout() {
        return createLayout({
            rows: [
                createToolbar("FMU-API"),
                {
                    cols: [
                        createSidebar(
                            Object.values(MENU_ITEMS),
                            (id) => this.router.navigate(id, this.bodyId)
                        ),
                        { id: this.bodyId }
                    ]
                }
            ]
        });
    }

    init() {
        InitProxy();

        webix.ready(() => {
            webix.ui(this.createMainLayout());

            this.router.navigate("config", "body");

            // Обработчик изменения размера окна
            webix.event(window, "resize", () => {
                const root = $$("root");
                const body = $$(this.bodyId);

                root.$setSize(window.innerWidth, window.innerHeight);
            });
        });
    }
}

// Инициализация приложения
const app = new App();
app.init();