/**
 * Менеджер для управления всеми polling операциями в приложении
 * Обеспечивает централизованное управление и автоматическую очистку при переключении страниц
 */
export class PollingManager {
    constructor() {
        this.activePollings = new Map(); // Хранилище активных polling'ов
    }

    register(id, pollingFunction, interval, options = {}) {
        this.stop(id);

        const pollingData = {
            id,
            pollingFunction,
            interval,
            options,
            intervalId: null,
            isRunning: false,
            startTime: Date.now()
        };

        this.activePollings.set(id, pollingData);

        if (options.autoStart !== false) {
            this.start(id);
        }

        console.log(`Polling зарегистрирован: ${id}, интервал: ${interval}ms`);
        return pollingData;
    }

    start(id) {
        const polling = this.activePollings.get(id);
        if (!polling) {
            console.warn(`Polling не найден: ${id}`);
            return;
        }

        if (polling.isRunning) {
            console.log(`Polling уже запущен: ${id}`);
            return;
        }

        const executePolling = async () => {
            if (!polling.isRunning) return;

            try {
                await polling.pollingFunction();
            } catch (error) {
                console.error(`Ошибка в polling ${id}:`, error);
            }

            if (polling.isRunning) {
                polling.intervalId = setTimeout(executePolling, polling.interval);
            }
        };

        polling.isRunning = true;
        
        const initialDelay = polling.options.initialDelay || 0;
        polling.intervalId = setTimeout(executePolling, initialDelay);

        console.log(`Polling запущен: ${id}`);
    }

    stop(id) {
        const polling = this.activePollings.get(id);
        if (!polling) return;

        if (!polling.isRunning) 
            return;

        if (polling.intervalId) {
            clearTimeout(polling.intervalId);
            polling.intervalId = null;
        }

        polling.isRunning = false;
        console.log(`Polling остановлен: ${id}`);
    }

    unregister(id) {
        this.stop(id);
        this.activePollings.delete(id);
        console.log(`Polling удален: ${id}`);
    }

    stopAll() {
        for (const [id, polling] of this.activePollings) {
            this.stop(id);
        }
    }

    clear() {
        this.stopAll();
        this.activePollings.clear();
        console.log("Все polling'и очищены");
    }

    getInfo(id) {
        const polling = this.activePollings.get(id);
        if (!polling) return null;

        return {
            id: polling.id,
            isRunning: polling.isRunning,
            interval: polling.interval,
            startTime: polling.startTime,
            uptime: Date.now() - polling.startTime
        };
    }

    getAllInfo() {
        const result = {};
        for (const [id, polling] of this.activePollings) {
            result[id] = this.getInfo(id);
        }
        return result;
    }
}

export const pollingManager = new PollingManager();