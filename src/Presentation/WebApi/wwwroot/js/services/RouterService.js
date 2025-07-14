export class RouterService {
    constructor() {
        this.currentPage = "";
        this.routes = new Map();
    }

    register(id, viewFactory) {
        this.routes.set(id, viewFactory);
    }

    async navigate(id, bodyId) {
        if (id === this.currentPage) return;

        const viewFactory = this.routes.get(id);
        if (!viewFactory) return;

        try {
            // Получаем view через await, так как viewFactory возвращает Promise
            const view = await viewFactory();
            
            // view теперь функция из module.default
            webix.ui(view(bodyId), $$(bodyId));
            $$(bodyId).show();

            this.currentPage = id;
        } catch (error) {
            console.error("Ошибка при навигации:", error);
        }
    }
}