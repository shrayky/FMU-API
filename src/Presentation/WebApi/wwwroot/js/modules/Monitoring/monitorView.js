import { ApiServerAddress } from '../../utils/net.js';

class MonitorView {
    constructor(id) {
        this.formName = "MonitorView";
        this.id = id;

        this.monitoringApiAddress = "/api/monitoring/systemstate"
        this.POLL_INTERVAL = 10000;

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
            url: "Адрес модуля"
        }
        this.NAMES = {
            toolbarLabel: "toolbarLabel",
            dbStatus: "dbStatus",
            localModulesTable: "localModulesTable"
        }

        this._startLocalMonitoringPolling();
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
                id: "dbStatus",
                label: this.LABELS.dbStatus + this.LABELS.dbStatusUnknown
            },
            { 
                view: "label", 
                label: this.LABELS.localModules
            },
            this._localModules(),
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
                    format: function(value) {
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
                    template: function(obj) {
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

    _startLocalMonitoringPolling() {
        const POLL_INTERVAL = this.POLL_INTERVAL;
        
        const pollStatus = async () => {
            try {
                const response = await fetch(this.monitoringApiAddress);
                
                if (!response.ok)
                    throw new Error(`Ошибка получения данных мониторинга ${response.status}`);

                const monitoringData = await response.json();
                
                this._updateDbState(monitoringData.couchDbOnLine);
                this._updateLocalModulesInformation(monitoringData.localeModulesInformation);

            } catch (error) {
                console.error("Ошибка при получении данных о состоянии сервиса:", error);
            }
        };

        pollStatus();
        
        const intervalId = setInterval(pollStatus, POLL_INTERVAL);

        this.on_destroy = () => {
            clearInterval(intervalId);
        };
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
        
        const tableData = Object.entries(localModulesInfo).map(([url, info]) => {
            return {
                id: url,
                url: url,
                version: info.version || "Нет данных",
                status: info.status || "Нет данных",
                lastSyncDateTime: info.lastSyncDateTime,
                isReady: info.isReady,
                hasSyncError: info.hasSyncError
            };
        });

        table.clearAll();
        table.parse(tableData);
    }
}

export default function (id) {
    return new MonitorView(id)
        .loadConfig()
        .render();
}
