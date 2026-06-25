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
            tsPiot: "Модули ТСПИоТ",
            tspiotName: "Имя",
            tspiotAddress: "Адрес",
            tspiotProtocolVersion: "Протокол",
            tspiotOnline: "Онлайн",
            tspiotLastCheck: "Последняя проверка",
            tspiotLicenseActiveTill: "Срок лицензии"
        }
        this.NAMES = {
            toolbarLabel: "toolbarLabel",
            dbStatus: "dbStatus",
            localModulesTable: "localModulesTable",
            tsPiotTable: "tsPiotTable",
            checkStatisticsTable: "checkStatisticsTable",
            checkStatisticsTableLabel: "checkStatisticsTableLabel",
            pollingLabel: "pollingLabel",
            tsPiotTableLabel: "tsPiotTableLabel",
        }
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

            ...this._tspiot(),
            ...this._localModules(),
            ...this._checkStatistics(),

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

    _tspiot() {
        return [
            {
                view: "label",
                label: this.LABELS.tsPiot,
                id: this.NAMES.tsPiotTableLabel
            },
            {
                id: this.NAMES.tsPiotTable,
                view: "datatable",
                css: "webix_data_border",
                columns: [
                    {
                        id: "name",
                        header: this.LABELS.tspiotName,
                        width: 220,
                        fillspace: true,
                        sort: "string"
                    },

                    {
                        id: "address",
                        header: this.LABELS.tspiotAddress,
                        width: 220,
                        sort: "string"
                    },
                    {
                        id: "protocolVersion",
                        header:
                        {
                            text: this.LABELS.tspiotProtocolVersion,
                            css: { "text-align": "center" }
                        },
                        width: 100,
                        sort: "number",
                        css: { "text-align": "center" }
                    },

                    {
                        id: "version",
                        header:
                        {
                            text: this.LABELS.version,
                            css: { "text-align": "center" }
                        },
                        width: 160,
                        sort: "string",
                        css: { "text-align": "center" }
                    },

                    {
                        id: "online",
                        header:
                        {
                            text: this.LABELS.tspiotOnline,
                            css: { "text-align": "center" }
                        },
                        width: 100,
                        sort: "string",
                        css: { "text-align": "center" },
                        template: function (obj) {
                            var color = obj.online ? "#00BFFF" : "#ff0000";
                            var text = obj.online ? "Да" : "Нет";
                            return `<span style="color: ${color};">${text}</span>`;
                        }
                    },

                    {
                        id: "licenseActiveTill",
                        header:
                        {
                            text: this.LABELS.tspiotLicenseActiveTill,
                            css: { "text-align": "center" }
                        },
                        width: 200,
                        sort: "date",
                        css: { "text-align": "center" },
                        format: function (value) {
                            if (!value) return "Нет данных";
                            const date = new Date(value);
                            if (date.getFullYear() <= 1970) return "Нет данных";
                            return date.toLocaleDateString();
                        }
                    },

                    {
                        id: "lastCheckTime",
                        header:
                        {
                            text: this.LABELS.tspiotLastCheck,
                            css: { "text-align": "center" }
                        },
                        width: 200,
                        sort: "date",
                        css: { "text-align": "center" },
                        format: function (value) {
                            const date = new Date(value);
                            if (date.getFullYear() <= 1970) return "Нет данных";
                            return date.toLocaleString();
                        }
                    },

                ],
                autoheight: true,
                scroll: false,
                select: false,
                data: []
            }
        ];
    }

    _localModules() {
        return [
            {
                view: "label",
                label: this.LABELS.localModules
            },
            {
                id: this.NAMES.localModulesTable,
                view: "datatable",
                css: "webix_data_border",
                columns: [
                    {
                        id: "url",
                        header: this.LABELS.url,
                        fillspace: true,
                        sort: "string"
                    },
                    {
                        id: "version",
                        header: 
                        {
                            text: this.LABELS.version,
                            css: { "text-align": "center" }
                        },
                        width: 150,
                        sort: "string",
                        css: { "text-align": "center" }
                    },
                    {
                        id: "lastSyncDateTime",
                        header: 
                        {
                            text: this.LABELS.lastSync,
                            css: { "text-align": "center" }
                        },
                        width: 300,
                        sort: "date",
                        css: { "text-align": "center" },
                        format: function (value) {
                            const date = new Date(value);
                            if (date.getFullYear() <= 1970) return "Нет данных";
                            return date.toLocaleString();
                        }
                    },
                    {
                        id: "status",
                        header: 
                        {
                            text: this.LABELS.status,
                            css: { "text-align": "center" }
                        },
                        width: 120,
                        sort: "string",
                        css: { "text-align": "center" },
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
        ];
    }

    _checkStatistics() {
        return [
            {
                view: "label",
                id: this.NAMES.checkStatisticsTableLabel,
                label: this.LABELS.checkStatisticsLabel
            },
            {
                id: this.NAMES.checkStatisticsTable,
                view: "datatable",
                css: "webix_data_border",
                columns: [
                    {
                        id: "period",
                        header: this.LABELS.period,
                        sort: "string",
                        fillspace: true,
                    },
                    {
                        id: "totalChecks",
                        header:
                        {
                            text: this.LABELS.totalChecks,
                            css: { "text-align": "center" }
                        },
                        sort: "number",
                        css: { "text-align": "center" }
                    },
                    {
                        id: "successfulOnline",
                        header:
                        {
                            text: this.LABELS.successfulOnlineChecks,
                            css: { "text-align": "center" }
                        },
                        sort: "date",
                        css: { "text-align": "center" }
                    },
                    {
                        id: "successfulOffline",
                        header:
                        {
                            text: this.LABELS.successfulOfflineChecks,
                            css: { "text-align": "center" }
                        },
                        sort: "date",
                        css: { "text-align": "center" }
                    },
                    {
                        id: "successRate",
                        header:
                        {
                            text: this.LABELS.successRatePercentage,
                            css: { "text-align": "center" }
                        },
                        sort: "date",
                        css: { "text-align": "center" }
                    }
                ],
                autoheight: true,
                scroll: false,
                select: false,
                data: []
            }
        ];
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
                this._updateTspiotInformation(monitoringData.tsPiotStates);
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
                hasSyncError: !module.isReady
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

        if (couchDbOnLine === "Disabled") {
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

    _updateTspiotInformation(tspiotInfo) {
        if (!tspiotInfo)
            return;

        const table = $$(this.NAMES.tsPiotTable);
        const tableLabel = $$(this.NAMES.tsPiotTableLabel);

        if (!table)
            return;

        const tableData = tspiotInfo.map((row, index) => {
            return {
                id: row.address || `tspiot_${index}`,
                name: row.name || "Неизвестно",
                address: row.address || "Неизвестно",
                protocolVersion: row.protocolVersion != null ? row.protocolVersion : "—",
                version: row.version || "Нет данных",
                online: !!row.online,
                lastCheckTime: row.lastCheckTime,
                licenseActiveTill: row.licenseActiveTill
            };
        });

        table.clearAll();

        if (tableData.length == 0) {
            table.clearAll();

            tableLabel.hide();
            table.hide();

            return;
        }

        table.show();
        tableLabel.show();

        table.parse(tableData);
    }
}

export default function (id) {
    const monitoring = new MonitorView(id).loadConfig();
    const view = monitoring.render();

    setTimeout(() => { monitoring.startLocalMonitoringPolling() }, 500);

    return view;
}
