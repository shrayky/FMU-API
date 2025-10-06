import { pollingManager } from '../../services/PollingManager.js';

class MonitorView {
    constructor(id) {
        this.formName = "MonitorView";
        this.id = id;

        this.monitoringApiAddress = "/api/monitoring/systemstate"
        this.POLL_INTERVAL = 30_000; // каждые 30 секунд
        this.isPolling = false;

        this.LABELS = {
            formTitle: "FMU-API: Мониторинг",
            dbStatus: "Статус базы данных: ",
            dbStatusOnline: "On-line",
            dbStatusOffline: "Off-line",
            dbStatusDisabled: "Disabled",
            dbStatusUnknown: "Неизвестно",
            localModules: "Локальные модули",
            lastSync: "Последняя синхронизация",
            version: "Версия",
            status: "Статус",
            url: "Адрес модуля",
            checkStatistics: "Статистика проверок",
            totalChecks: "Всего проверок",
            successfulOnlineChecks: "Онлайн",
            successfulOfflineChecks: "Офлайн",
            successRatePercentage: "Успешно(%)",
            today: "Сегодня",
            last7Days: "Последние 7 дней",
            last30Days: "Последние 30 дней",
            period: "Период",
            checkStatisticsLabel: "Статистика проверок",
            pollingLabel: "⏳ Обновление...",
        }
        this.NAMES = {
            toolbarLabel: "toolbarLabel",
            dbStatus: "dbStatus",
            localModulesTable: "localModulesTable",
            checkStatisticsTable: "checkStatisticsTable",
            checkStatisticsTableLabel: "checkStatisticsTableLabel",
            pollingLabel: "pollingLabel",
        }

        //this._startLocalMonitoringPolling();
    }

    loadConfig() {
        this.CouchDbOnLine = "Off-line";
        return this;
    }

    render() {
        $$("toolbarLabel").setValue(this.LABELS.formTitle);

        var formElements = [
            {
                view: "label",
                id: this.NAMES.pollingLabel,
                name: this.NAMES.pollingLabel,
                label: this.LABELS.pollingLabel,
                hidden: true,
            },

            {
                view: "label",
                id: "dbStatus",
                label: this.LABELS.dbStatus + this.LABELS.dbStatusUnknown
            },

            {
                view: "label",
                label: this.LABELS.localModules
            },

            this._localModules(),

            {
                view: "label",
                id: this.NAMES.checkStatisticsTableLabel,
                label: this.LABELS.checkStatisticsLabel
            },

            this._checkStatistics(),

            {}
        ];

        var form = {
            view: "form",
            id: this.id,
            name: this.formName,
            disabled: false,
            elements: formElements
        }

        return form;
    }

    _localModules() {
        return {
            id: this.NAMES.localModulesTable,
            view: "datatable",
            columns: [
                {
                    id: "url",
                    header: this.LABELS.url,
                    fillspace: true,
                    sort: "string"
                },
                {
                    id: "version",
                    header: this.LABELS.version,
                    width: 150,
                    sort: "string"
                },
                {
                    id: "lastSyncDateTime",
                    header: this.LABELS.lastSync,
                    width: 300,
                    sort: "date",
                    format: function (value) {
                        const date = new Date(value);
                        if (date.getFullYear() <= 1970) return "Нет данных";
                        return date.toLocaleString();
                    }
                },
                {
                    id: "status",
                    header: this.LABELS.status,
                    width: 120,
                    sort: "string",
                    template: function (obj) {
                        var color = obj.isReady ? "#00BFFF" : "#ff0000";
                        return `<span style="color: ${color};">${obj.status || "Нет данных"}</span>`;
                    }
                },
            ],
            autoheight: true,
            scroll: false,
            select: false,
            data: []
        }
    }

    _checkStatistics() {
        return {

            id: this.NAMES.checkStatisticsTable,
            view: "datatable",
            columns: [
                {
                    id: "period",
                    header: this.LABELS.period,
                    sort: "string",
                    fillspace: true,
                },
                {
                    id: "totalChecks",
                    header: this.LABELS.totalChecks,
                    sort: "number",
                },
                {
                    id: "successfulOnline",
                    header: this.LABELS.successfulOnlineChecks,
                    sort: "date",
                },
                {
                    id: "successfulOffline",
                    header: this.LABELS.successfulOfflineChecks,
                    sort: "date",
                },
                {
                    id: "successRate",
                    header: this.LABELS.successRatePercentage,
                    sort: "date",
                },
                {}
            ],
            autoheight: true,
            scroll: false,
            select: false,
            data: []

        }
    }

    startLocalMonitoringPolling() {
        const POLL_INTERVAL = this.POLL_INTERVAL;
        
        const pollStatus = async () => {

            if (this.isPolling) {
                console.log("pol already running");
                return;
            }


            const indicator = $$(this.NAMES.pollingLabel);
            
            if (indicator)
                indicator.show();

            try {              

                const response = await fetch(this.monitoringApiAddress);

                if (!response.ok)
                    throw new Error(`Ошибка получения данных мониторинга ${response.status}`);

                const monitoringData = await response.json();

                this._updateDbState(monitoringData.couchDbOnLine);
                this._updateLocalModulesInformation(monitoringData.stateOfLocalModules);
                this._updateCheckStatistics(monitoringData.markCheksStatistics, monitoringData.couchDbOnLine);

            } catch (error) {
                if (error.name === 'TypeError' || 
                    error.message.includes('fetch') || 
                    error.message.includes('Failed to fetch') ||
                    error.message.includes('NetworkError') ||
                    error.message.includes('ERR_CONNECTION_REFUSED')) {
                    return;
                }
                
                console.error("Ошибка при получении данных о состоянии сервиса:", error);
            }

            if (indicator)
                indicator.hide();
        };

        pollingManager.register(
            'system-monitoring-polling', 
            pollStatus, 
            this.POLL_INTERVAL,
            { 
                initialDelay: 1000,
                autoStart: true 
            }
        );


    }

    _updateDbState(couchDbOnLine) {
        const couchDbState = $$(this.NAMES.dbStatus);

        if (!couchDbState)
            return;

        if (couchDbOnLine == "Disabled") {
            couchDbState.setValue(this.LABELS.dbStatus + `<span style="color: #FFFFFF;">${this.LABELS.dbStatusDisabled}</span>`);
        }
        else
            couchDbState.setValue(this.LABELS.dbStatus + (couchDbOnLine == "On-line" ?
                `<span style="color: #00BFFF;">${this.LABELS.dbStatusOnline}</span>` :
                `<span style="color: #ff0000;">${this.LABELS.dbStatusOffline}</span>`));
    }

    _updateLocalModulesInformation(localModulesInfo) {
        if (!localModulesInfo)
            return;

        console.log(localModulesInfo);

        const table = $$(this.NAMES.localModulesTable);

        if (!table)
            return;

        const tableData = localModulesInfo.map((module, index) => {
            return {
                id: module.address || `module_${index}`,
                url: module.address || "Неизвестно",
                version: module.version || "Нет данных",
                status: module.state || "Нет данных",
                lastSyncDateTime: module.lastSyncTime,
                isReady: module.isReady || false,
                hasSyncError: !module.isReady // Предполагаем ошибку если модуль не готов
            };
        });

        table.clearAll();
        table.parse(tableData);
    }

    _updateCheckStatistics(checkStatistics, couchDbOnLine) {
        const table = $$(this.NAMES.checkStatisticsTable);
        const tableLabel = $$(this.NAMES.checkStatisticsTableLabel);

        if (!table)
            return;

        if (couchDbOnLine == "Disabled") {
            table.hide();
            tableLabel.hide();
            return;
        }
            
        table.clearAll();

        const statisticsData = [
            {
                id: "today",
                period: this.LABELS.today,
                totalChecks: checkStatistics.today.total || 0,
                successfulOnline: checkStatistics.today.successfulOnline || 0,
                successfulOffline: checkStatistics.today.successfulOffline || 0,
                successRate: checkStatistics.today.successRate || 0
            },
            {
                id: "last7Days",
                period: this.LABELS.last7Days,
                totalChecks: checkStatistics.last7Days.total || 0,
                successfulOnline: checkStatistics.last7Days.successfulOnline || 0,
                successfulOffline: checkStatistics.last7Days.successfulOffline || 0,
                successRate: checkStatistics.last7Days.successRate || 0
            },
            {
                id: "last30Days",
                period: this.LABELS.last30Days,
                totalChecks: checkStatistics.last30Days.total || 0,
                successfulOnline: checkStatistics.last30Days.successfulOnline || 0,
                successfulOffline: checkStatistics.last30Days.successfulOffline || 0,
                successRate: checkStatistics.last30Days.successRate || 0
            }
        ];

        table.parse(statisticsData);
    }
}

export default function (id) {
    const monitoring = new MonitorView(id).loadConfig();
    const view = monitoring.render();

    // отложенный старт для того что бы страница загрузилась полностью
    setTimeout(() => {monitoring.startLocalMonitoringPolling()}, 1_000);

    return view;
}
