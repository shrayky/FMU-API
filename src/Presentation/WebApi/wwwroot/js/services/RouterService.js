// js/services/RouterService.js
export class RouterService {
    constructor() {
        this.currentPage = "";
        this.routes = new Map();
    }

    register(id, viewFactory) {
        this.routes.set(id, viewFactory);
    }

    navigate(id, bodyId) {
        if (id === this.currentPage) return;

        const viewFactory = this.routes.get(id);
        if (!viewFactory) return;

        // Получаем конфигурацию view напрямую
        const view = viewFactory();
        
        //console.log(view(bodyId));
        webix.ui(view(bodyId), $$(bodyId));
        $$(bodyId).show();

        this.currentPage = id;
    }
}