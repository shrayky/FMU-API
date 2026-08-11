import { Label, TableToolbar, padding } from "../../../utils/ui.js";
import { saveConfiguration } from '../../../services/ConfigurationService.js';
import { pollingManager } from '../../../services/PollingManager.js';
import { showOrganizationForm, collectOrganizationFormData } from "./organizationForm.js";
import { openGisMtWindow } from "./gisMtWindow.js";

class OrganizationsConfigurationElement {
    constructor(id) {
        this.id = id;
        this.formName = "OrganisationForm";
        this.LABELS = {
            title: "Организации",
            newOrg: "Новая организация",
            editOrg: "Организация",
            code: "Код организации (группы печати), если группы печати не используются, то все равно код должен быть 1",
            inn: "ИНН организации",
            name: "Наименование",
            xapikey: "X-API key (действует до 1 октября 2026)",
            add: "Сохранить",
            close: "Закрыть",
            enable: "Используется",
            connectionAddress: "Адрес подключения",
            userName: "Имя пользователя",
            password: "Пароль",
            localModuleStatus: "Статус ЛМ",
            LocalModuleTitle: "Локальный модуль Честного знака",
            localModuleStatusTitle: "Статус локального модуля",
            localModuleInit: "Инициализация ЛМ",
            eniseyConnectionAddress: "Адрес подключения БД Енисей",
            tsPiotHost: "Адрес ТС ПИоТ (для Frontol ниже 28)",
            tsPiotPort: "Порт",
            signPassword: "Пароль от ЭЦП",
            LoadToken: "Получить токен",
            DigitalSignature: "Сертификат ЭЦП",
            gisMt: "ГИС МТ",
            gisMtWindowTitle: "ГИС МТ",
            productGroupsTab: "Группы",
            operationsTab: "Операции",
            loadProductGroups: "Загрузить группы",
            syncDocuments: "Загрузить документы",
            loadStock: "Загрузить остатки",
            tsPiotInformationPort: "Порт",
            tsPiotinformationEndpoint: "Эндпоинт запроса"
        };

        this.LOCAL_MODULE_STATUS = {
            NOT_CONFIGURED: 0,
            INITIALIZATION: 1,
            READY: 2,
            SYNC_ERROR: 3,
            ENISEY_OFF_LINE: 4,
            UNKNOWN: 5
        };

        this.LOCAL_MODULE_STATUS_DISPLAY = {
            [this.LOCAL_MODULE_STATUS.NOT_CONFIGURED]: { text: "Не настроен", color: "#FFA500" },
            [this.LOCAL_MODULE_STATUS.INITIALIZATION]: { text: "Инициализация", color: "#3498DB" },
            [this.LOCAL_MODULE_STATUS.READY]: { text: "Готов к работе", color: "#2ECC71" },
            [this.LOCAL_MODULE_STATUS.SYNC_ERROR]: { text: "Ошибка синхронизации", color: "#E74C3C" },
            [this.LOCAL_MODULE_STATUS.ENISEY_OFF_LINE]: { text: "Енисей off-line", color: "#E74C3C" },
            [this.LOCAL_MODULE_STATUS.UNKNOWN]: { text: "Неизвестный статус", color: "#E74C3C" }
        };

        this.POLL_INTERVAL = 10000;
        this._startLocalModuleStatusPolling();
    }

    _getStatusDisplay(organisationConfig) {
        if (!organisationConfig.localModuleConnection?.enable) {
            return { text: "Не подключен", color: "#FFA500" };
        }

        return this.LOCAL_MODULE_STATUS_DISPLAY[organisationConfig.localModuleStatus] || {
            text: "Неизвестный статус",
            color: "#95A5A6"
        };
    }

    loadConfig(config) {
        if (config && config.organisationConfig && config.organisationConfig.printGroups) {
            this.printGroups = config.organisationConfig.printGroups.map(group => ({
                ...group,
                trueApiIntegrationSettings: {
                    ...(group.trueApiIntegrationSettings ?? {}),
                    enable: !!(group.trueApiIntegrationSettings?.enable)
                },
                localModuleStatus: this.LOCAL_MODULE_STATUS.NOT_CONFIGURED
            }));
        }

        return this;
    }

    render() {
        const elements = [];
        elements.push(Label("lOrganizations", this.LABELS.title));

        const toolbar = TableToolbar("PrintGroups");

        toolbar.cols.splice(toolbar.cols.length - 1, 0, {
            view: "button",
            value: this.LABELS.gisMt,
            id: "gisMt_PrintGroups",
            disabled: true,
            width: 120,
            tooltip: "Работа с ГИС МТ",
            click: () => this._openGisMtWindow()
        });

        toolbar.cols.splice(toolbar.cols.length - 1, 0, {
            view: "button",
            value: this.LABELS.localModuleInit,
            id: "initLm_PrintGroups",
            disabled: true,
            width: 200,
            tooltip: "Инициализация локального модуля",
            click: () => this._initializeLocalModule()
        });

        elements.push({
            padding: padding,
            name: "organisationConfig",
            rows: [
                toolbar,
                this._createFormTable()
            ]
        });

        return { id: this.id, rows: elements };
    }

    _createFormTable() {
        return {
            view: "formtable",
            id: "PrintGroups",
            name: "organisationConfig.printGroups",
            data: this.printGroups,
            resizeColumn: true,
            resizeRow: true,
            select: true,
            minHeight: 250,
            columns: [
                { id: "id", header: this.LABELS.code },
                { id: "name", header: this.LABELS.name, fillspace: true },
                { id: "inn", header: this.LABELS.inn, fillspace: true },
                {
                    id: "localModuleStatus",
                    header: this.LABELS.localModuleStatusTitle,
                    fillspace: true,
                    template: (obj) => {
                        const status = this._getStatusDisplay(obj);
                        return `<div style="
                            color: ${status.color};
                            font-weight: bold;
                            text-align: center;
                            padding: 2px 5px;
                            border-radius: 3px;
                            background: ${status.color}15;
                        ">${status.text}</div>`;
                    }
                }
            ],
            on: {
                onAfterSelect: () => {
                    $$("delete_PrintGroups").enable();
                    this._updateInitButtonState();
                },
                onAfterDelete: () => {
                    $$("delete_PrintGroups").disable();
                    if ($$("PrintGroups").count() == 0)
                        $$("deleteAll_PrintGroups").disable();
                    this._updateInitButtonState();
                },
                onBeforeAdd: (id, obj) => {
                    if (obj.xapikey == undefined) {
                        this.showForm(this.LABELS.newOrg, "PrintGroups");
                        return false;
                    }
                },
                onItemDblClick: (id) => {
                    this.showForm(this.LABELS.editOrg, "PrintGroups", id);
                }
            }
        };
    }

    showForm(label, tableId, id) {
        showOrganizationForm({
            labels: this.LABELS,
            formName: this.formName,
            tableId,
            id,
            onSave: (tid, rowId) => this._handleAddButton(tid, rowId)
        });
    }

    _handleAddButton(tableId, id) {
        const organizationId = $$("OrganizationId").getValue();
        if (organizationId == "") return;

        const table = $$(tableId);
        if (table == undefined) return;

        const existRows = table.find(obj => obj.id == organizationId && organizationId != id);
        if (existRows.length > 0) {
            webix.message({
                text: "Организация с таким кодом уже есть в списке!",
                type: "error"
            });
            return;
        }

        const newData = collectOrganizationFormData(table, id);

        if (id == undefined)
            table.add(newData);
        else
            table.updateItem(id, newData);

        if (table.count() > 0)
            $$("deleteAll_PrintGroups").enable();

        $$(this.formName).close();
        this._saveConfiguration();
        this._updateInitButtonState();
    }

    _saveConfiguration() {
        saveConfiguration("body");
    }

    _updateInitButtonState() {
        const selectedId = $$("PrintGroups").getSelectedId();
        const initButton = $$("initLm_PrintGroups");
        const gisMtButton = $$("gisMt_PrintGroups");

        if (!selectedId) {
            if (initButton) initButton.disable();
            if (gisMtButton) gisMtButton.disable();
            return;
        }

        const item = $$("PrintGroups").getItem(selectedId);

        if (initButton) {
            const connection = item.localModuleConnection ?? {};
            const isEnabled = connection.enable &&
                connection.connectionAddress &&
                connection.userName &&
                connection.password;
            if (isEnabled) initButton.enable();
            else initButton.disable();
        }

        if (gisMtButton) {
            if (!!item.trueApiIntegrationSettings?.enable)
                gisMtButton.enable();
            else
                gisMtButton.disable();
        }
    }

    _openGisMtWindow() {
        const selectedId = $$("PrintGroups").getSelectedId();
        if (!selectedId) {
            webix.message({ text: "Выберите организацию", type: "warning" });
            return;
        }

        const item = $$("PrintGroups").getItem(selectedId);
        if (!item.trueApiIntegrationSettings?.enable) {
            webix.message({ text: "Включите True API интеграцию для организации", type: "warning" });
            return;
        }

        openGisMtWindow({
            item,
            selectedId,
            labels: this.LABELS,
            onSaveConfiguration: () => this._saveConfiguration()
        });
    }

    _startLocalModuleStatusPolling() {
        pollingManager.register(
            "localModuleStatus",
            async () => {
                try {
                    const response = await fetch('/api/lm/state');
                    if (!response.ok)
                        throw new Error('Ошибка получения статусов');

                    const states = await response.json();
                    const table = $$("PrintGroups");
                    if (!table) return;

                    // API: [{ organization, status }, ...]
                    states.forEach(({ organization, status }) => {
                        if (!table.exists(organization)) return;

                        const item = table.getItem(organization);
                        table.updateItem(organization, {
                            ...item,
                            localModuleStatus: status
                        });
                    });
                } catch (error) {
                    console.error("Ошибка при получении статусов ЛМ:", error);
                }
            },
            this.POLL_INTERVAL,
            { autoStart: true }
        );
    }

    _initializeLocalModule() {
        const selectedId = $$("PrintGroups").getSelectedId();
        if (!selectedId) {
            webix.message({
                text: "Выберите организацию для инициализации локального модуля",
                type: "warning"
            });
            return;
        }

        const table = $$("PrintGroups");
        const item = table.getItem(selectedId);

        if (!item.localModuleConnection?.enable)
            return;

        if (item.localModuleStatus === this.LOCAL_MODULE_STATUS.NOT_CONFIGURED
            || item.localModuleStatus === this.LOCAL_MODULE_STATUS.SYNC_ERROR) {
            this._startInitialization(selectedId, item);
            return;
        }

        if (item.localModuleStatus != this.LOCAL_MODULE_STATUS.READY) {
            webix.message({
                text: `В этом статусе ЛМ ${this.LOCAL_MODULE_STATUS_DISPLAY[item.localModuleStatus].text} нельзя выполнять инициализацию!`,
                type: "warning"
            });
            return;
        }

        webix.confirm({
            title: "Инициализация локального модуля",
            text: `Выполнить повторную инициализацию локального модуля для организации "${item.name}"?`,
            ok: "Да",
            cancel: "Отмена",
            callback: (result) => {
                if (result)
                    this._startInitialization(selectedId, item);
            }
        });
    }

    _startInitialization(selectedId, item) {
        const table = $$("PrintGroups");
        table.updateItem(selectedId, {
            ...item,
            localModuleStatus: this.LOCAL_MODULE_STATUS.INITIALIZATION
        });

        fetch(`/api/lm/init/${selectedId}`, { method: 'POST' })
            .catch(error => {
                console.error("Ошибка при отправке запроса инициализации:", error);
                webix.message({
                    text: "Ошибка при отправке запроса инициализации",
                    type: "error"
                });
            });

        webix.message({
            text: "Запущена инициализация локального модуля",
            type: "info"
        });
    }
}

export default function (id, config) {
    return new OrganizationsConfigurationElement(id)
        .loadConfig(config)
        .render();
}
