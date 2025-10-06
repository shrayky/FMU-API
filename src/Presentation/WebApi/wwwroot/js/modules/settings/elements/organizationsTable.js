import { Label, TableToolbar, Text, Number, padding, PasswordBox, CheckBox } from "../../../utils/ui.js";
import { saveConfiguration } from '../../../utils/saveConfiguration.js';
import { pollingManager } from '../../../services/PollingManager.js';


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
            xapikey: "X-API key",
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
        };

        this.LOCAL_MODULE_STATUS = {
            NOT_CONFIGURED: 0,
            INITIALIZATION: 1,
            READY: 2,
            SYNC_ERROR: 3,
            UNKNOWN: 4
        };

        this.LOCAL_MODULE_STATUS_DISPLAY = {
            [this.LOCAL_MODULE_STATUS.NOT_CONFIGURED]: {
                text: "Не настроен",
                color: "#FFA500" // Orange
            },
            [this.LOCAL_MODULE_STATUS.INITIALIZATION]: {
                text: "Инициализация",
                color: "#3498DB" // Blue
            },
            [this.LOCAL_MODULE_STATUS.READY]: {
                text: "Готов к работе",
                color: "#2ECC71" // Green
            },
            [this.LOCAL_MODULE_STATUS.SYNC_ERROR]: {
                text: "Ошибка синхронизации",
                color: "#E74C3C" // Red
            },
            [this.LOCAL_MODULE_STATUS.UNKNOWN]: {
                text: "Неизвестный статус",
                color: "#E74C3C" // Gray
            }
        };

        this.POLL_INTERVAL = 10000;

        this._startLocalModuleStatusPolling();
    }

    _getStatusDisplay(lmStatus) {
        return this.LOCAL_MODULE_STATUS_DISPLAY[lmStatus] || {
            text: "Неизвестный статус",
            color: "#95A5A6"
        };
    };

    loadConfig(config) {
        if (config && config.organisationConfig && config.organisationConfig.printGroups) {
            this.printGroups = config.organisationConfig.printGroups.map(group => ({
                ...group,
                localModuleStatus: this.LOCAL_MODULE_STATUS.NOT_CONFIGURED
            }));
        }

        return this;
    }

    render() {
        var elements = [];

        elements.push(
            Label("lOrganizations", this.LABELS.title)
        );

        const toolbar = TableToolbar("PrintGroups");
        toolbar.cols.splice(toolbar.cols.length - 1, 0, {
            view: "button",
            value: this.LABELS.localModuleInit,
            id: "initLm_PrintGroups",
            disabled: true,
            tableId: "PrintGroups",
            autowidth: false,
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
                { id: "xapikey", header: this.LABELS.xapikey, fillspace: true },
                { 
                    id: "localModuleStatus",
                    header: this.LABELS.localModuleStatusTitle,

                    fillspace: true,
                    template: (obj) => {
                        const status = this._getStatusDisplay(obj.localModuleStatus);
                        return `<div style="
                            color: ${status.color}; 
                            font-weight: bold; 
                            text-align: center;
                            padding: 2px 5px;
                            border-radius: 3px;
                            background: ${status.color}15;
                        ">
                            ${status.text}
                        </div>`;
                    }
                }
            ],
            on: {

                onAfterSelect: (selection) => {
                    $$("delete_PrintGroups").enable();
                    this._updateInitButtonState();
                },
                onAfterDelete: (id) => {
                    $$("delete_PrintGroups").disable();
                    if ($$("PrintGroups").count() == 0)
                        $$("deleteAll_PrintGroups").disable();
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
        const windowInnerWidth = window.innerWidth;

        webix.ui({
            view: "window",
            id: this.formName,
            position: "center",
            modal: true,
            move: false,
            resize: false,
            width: windowInnerWidth * 0.8,
            head: this._createFormHeader(label),
            body: this._createFormBody(tableId, id)
        }).show();

        this._initFormValues(tableId, id);
    }

    _createFormHeader(label) {
        return {
            view: "toolbar",
            elements: [
                {
                    view: "label",
                    label: label,
                },
                {
                    view: "icon",
                    icon: "wxi-close",
                    click: () => $$(this.formName).close()
                }
            ]
        };
    }

    _createFormBody(tableId, id) {
        return {
            padding: 10,
            rows: [
                Number(this.LABELS.code, "OrganizationId", "1111"),
                Text(this.LABELS.name, "OrganizationName"),
                Text(this.LABELS.inn, "OrganizationInn"),
                Text(this.LABELS.xapikey, "XAPIKEY"),
              
                Label("LocalModuleTitle", this.LABELS.LocalModuleTitle),
                CheckBox(this.LABELS.enable, "LocalModuleEnable"),
                Text(this.LABELS.connectionAddress, "LocalModuleConnectionAddress", "", {
                    placeholder: "http://hostname:5995"
                }),
                Text(this.LABELS.userName, "LocalModuleUserName"),
                PasswordBox(this.LABELS.password, "LocalModulePassword"),

                {
                    padding: {
                        top: 10
                    },
                    cols: [
                        {
                            view: "button",
                            value: this.LABELS.add,
                            id: "addButton",
                            autowidth: "false",
                            width: 400,
                            click: () => this._handleAddButton(tableId, id)
                        },
                        {
                            view: "button",
                            value: this.LABELS.close,
                            id: "closeBtn",
                            autowidth: "false",
                            width: 400,
                            click: () => $$(this.formName).close()
                        },
                        {}
                    ]
                }
            ]
        };
    }

    _handleAddButton(tableId, id) {
        let organizationId = $$("OrganizationId").getValue();
        if (organizationId == "") return;

        let table = $$(tableId);
        if (table == undefined) return;

        let existRows = table.find(obj => obj.id == organizationId && organizationId != id);
        if (existRows.length > 0) {
            webix.message({
                text: "Организация с таким кодом уже есть в списке!",
                type: "error"
            });
            return;
        }

        const newData = {
            id: organizationId,
            xapikey: $$("XAPIKEY").getValue(),
            inn: $$("OrganizationInn").getValue(),
            name: $$("OrganizationName").getValue(),
            localModuleConnection: {
                enable: $$("LocalModuleEnable").getValue(),
                connectionAddress: $$("LocalModuleConnectionAddress").getValue(),
                userName: $$("LocalModuleUserName").getValue(),
                password: $$("LocalModulePassword").getValue()
            }
        };

        if (id == undefined) {
            table.add(newData);

        } else {
            table.updateItem(id, newData);
        }

        if (table.count() > 0)
            $$("deleteAll_PrintGroups").enable();

        $$(this.formName).close();

        this._saveConfiguration();
        this._updateInitButtonState();
    }

    _saveConfiguration() {
        saveConfiguration("body");
    }

    _initFormValues(tableId, id) {
        let table = $$(tableId);

        if (id == undefined) {
            let lastId = table.getLastId();
            $$("OrganizationId").setValue(lastId == undefined ? 1 : lastId + 1);
            return
        }

        let item = table.getItem(id);

        $$("OrganizationId").setValue(item.id);
        $$("OrganizationId").disable();
        $$("OrganizationInn").setValue(item.inn);
        $$("OrganizationName").setValue(item.name);
        $$("LocalModuleEnable").setValue(item.localModuleConnection.enable);
        $$("LocalModuleConnectionAddress").setValue(item.localModuleConnection.connectionAddress);
        $$("LocalModuleUserName").setValue(item.localModuleConnection.userName);
        $$("LocalModulePassword").setValue(item.localModuleConnection.password);
        $$("XAPIKEY").setValue(item.xapikey);
    }

    _startLocalModuleStatusPolling() {
        const POLL_INTERVAL = this.POLL_INTERVAL;
        
        const pollStatus = async () => {

            try {
                const response = await fetch('/api/lm/state');
                if (!response.ok)
                    throw new Error('Ошибка получения статусов');

                const states = await response.json();
                
                const table = $$("PrintGroups");

                if (!table)
                    return;

                states.forEach(state => {
                    const item = table.getItem(state.organization);

                    if (item) {
                        if (item.localModuleStatus == state.status)
                            return;

                        const updatedItem = {
                            ...item,
                            localModuleStatus: state.status,
                        };

                        table.updateItem(state.organization, updatedItem);
                        this._updateInitButtonState();
                    }
                });

            } catch (error) {
                if (error.name === 'TypeError' || 
                    error.message.includes('fetch') || 
                    error.message.includes('Failed to fetch') ||
                    error.message.includes('NetworkError') ||
                    error.message.includes('ERR_CONNECTION_REFUSED')) {
                    return;
                }
                console.error("Ошибка при получении статусов ЛМ:", error);
            }
        };

        pollingManager.register(
            'localmodules-state-polling', 
            pollStatus, 
            this.POLL_INTERVAL,
            { 
                initialDelay: 1000,
                autoStart: true 
            }
        );
        
    }

    _updateInitButtonState() {
        const selectedId = $$("PrintGroups").getSelectedId();
        const initButton = $$("initLm_PrintGroups");
        
        if (!selectedId || !initButton) return;

        const item = $$("PrintGroups").getItem(selectedId);
        const connection = item.localModuleConnection;

        // Проверяем все условия для активации кнопки
        const isEnabled = connection.enable && 
            connection.connectionAddress && 
            connection.userName && 
            connection.password;

        if (isEnabled) {
            initButton.enable();
        } else {
            initButton.disable();
        }
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

        if (!item.localModuleConnection.enable)
            return;
        
        if (item.localModuleStatus === this.LOCAL_MODULE_STATUS.NOT_CONFIGURED) {
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
                if (result) {
                    this._startInitialization(selectedId, item);
                }
            }
        });
    }

    _startInitialization(selectedId, item) {
        const table = $$("PrintGroups");
        const updatedItem = {
            ...item,
            localModuleStatus: this.LOCAL_MODULE_STATUS.INITIALIZATION
        };
        table.updateItem(selectedId, updatedItem);
                
        fetch(`/api/lm/init/${selectedId}`, {method: 'POST'})
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