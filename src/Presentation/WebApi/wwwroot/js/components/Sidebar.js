import { isMobileDevice, isNarrowScreen } from '../utils/device.js';

const SIDEBAR_COLLAPSED_KEY = "sidebar_collapsed";
const RESIZE_DEBOUNCE_MS = 150;

function ensureIconsStyles() {
    if (document.querySelector("link[data-sidebar-icons]")) {
        return;
    }

    const link = document.createElement("link");
    link.rel = "stylesheet";
    link.href = "/css/icons.css";
    link.dataset.sidebarIcons = "true";
    document.head.appendChild(link);
}

ensureIconsStyles();

/**
 * Сайдбар с кнопкой сворачивания, сохранением состояния и автосворачиванием
 * на узких экранах.
 */
export class Sidebar {
    constructor({ items, onSelect, logoText }) {
        this.items = items;
        this.onSelect = onSelect;
        this.logoText = logoText;
        this.sidebarId = "mainSidebar";
        this.logoId = "logoText";
        this.columnId = "sidebarColumn";
        this.expandedWidth = 220;
        this.collapsedWidth = 44;
        this._resizeTimer = null;
        this._viewportHandlerAttached = false;
    }

    getView() {
        ensureIconsStyles();
        const collapsed = this._getCollapsed();

        this._attachViewportHandler();

        return {
            id: this.columnId,
            width: collapsed ? this.collapsedWidth : this.expandedWidth,
            rows: [
                {
                    padding: 2,
                    border: 1,
                    height: 60,
                    cols: [
                        {
                            view: "label",
                            id: this.logoId,
                            label: this.logoText,
                            css: "webix_primary",
                            minWidth: 0,
                            width: this.expandedWidth - this.collapsedWidth - 4,
                            hidden: collapsed
                        },
                        {
                            view: "icon",
                            icon: "mdi mdi-menu",
                            width: this.collapsedWidth,
                            click: () => this._toggle()
                        }
                    ]
                },
                {
                    view: "sidebar",
                    id: this.sidebarId,
                    width: 220,
                    collapsed: collapsed,
                    position: this._isMobile() ? "right" : "left",
                    data: this.items,
                    on: {
                        onAfterSelect: this.onSelect
                    },
                    borderless: true
                }
            ]
        };
    }

    /**
     * Переключает свёрнутость сайдбара и скрывает заголовок в узкой колонке.
     */
    _toggle() {
        const sidebar = $$(this.sidebarId);
        sidebar.toggle();

        this._applyCollapsedState(sidebar.getState().collapsed);
    }

    /**
     * Следит за шириной окна: при сужении сайдбар сворачивается, при возврате
     * на широкий экран восстанавливается выбор пользователя.
     */
    _attachViewportHandler() {
        if (this._viewportHandlerAttached) {
            return;
        }

        this._viewportHandlerAttached = true;

        webix.event(window, "resize", () => {
            clearTimeout(this._resizeTimer);
            this._resizeTimer = setTimeout(() => this._syncToViewport(), RESIZE_DEBOUNCE_MS);
        });
    }

    _syncToViewport() {
        const sidebar = $$(this.sidebarId);

        if (!sidebar) {
            return;
        }

        const isCollapsed = sidebar.getState().collapsed;
        const shouldCollapse = isNarrowScreen() ? true : this._getSavedCollapsed();

        if (isCollapsed === shouldCollapse) {
            return;
        }

        if (shouldCollapse) {
            sidebar.collapse();
        } else {
            sidebar.expand();
        }

        this._applyCollapsedState(shouldCollapse);
    }

    _applyCollapsedState(isCollapsed) {
        this._saveCollapsed(isCollapsed);
        this._syncTitle(isCollapsed);
        this._syncColumnWidth(isCollapsed);
    }

    _syncColumnWidth(isCollapsed) {
        const column = $$(this.columnId);
        column.define("width", isCollapsed ? this.collapsedWidth : this.expandedWidth);
        column.resize();
    }

    _syncTitle(isCollapsed) {
        const logo = $$(this.logoId);

        if (isCollapsed) {
            logo.hide();
        } else {
            logo.show();
        }
    }

    _isMobile() {
        return isMobileDevice();
    }

    _getCollapsed() {
        if (isNarrowScreen()) {
            return true;
        }

        return this._getSavedCollapsed();
    }

    _getSavedCollapsed() {
        return localStorage.getItem(SIDEBAR_COLLAPSED_KEY) === "true";
    }

    /**
     * На узком экране автосвёрнутое состояние не сохраняется,
     * чтобы не затирать выбор пользователя.
     */
    _saveCollapsed(isCollapsed) {
        if (isNarrowScreen()) {
            return;
        }

        localStorage.setItem(SIDEBAR_COLLAPSED_KEY, isCollapsed.toString());
    }
}
