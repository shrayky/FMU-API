import { TableToolbar, Text, Number, padding, PasswordBox, CheckBox, Label } from "../../../utils/ui.js";
import { saveConfiguration } from "../../../services/ConfigurationService.js";
import { importFromFrontolAdmin } from "../../../services/FrontolConnectionService.js";
import { frontolDbValidation } from "../../../utils/validators.js";

class ConnectedFrontolConfigurationElement {
    constructor(id) {
        this.id = id;
        this.mainFormId = "body";
        this.modalId = "FrontolConnectionsModal";
        this.editFormId = "FrontolConnectionEditForm";
        this.tableId = "FrontolConnections";
        this.hiddenTableId = "FrontolConnectionsHidden";
        this.printGroupSelectId = "FrontolPrintGroupSourceId";

        this.LABELS = {
            title: "Настройка подключения к Frontol",
            openSettings: "Настроить",
            modalTitle: "Подключения к базам Frontol",
            syncBeerTaps: "Использовать",
            syncPeriod: "Период синхронизации (сек)",
            printGroupSource: "База для получения группы печати",
            importFromAdmin: "Импорт из Frontol.Администратор",
            newConnection: "Новое подключение",
            editConnection: "Подключение",
            name: "Наименование",
            path: "Путь к базе (сервер:путь\\файл.gdb)",
            user: "Имя пользователя Firebird",
            password: "Пароль пользователя Firebird",
            save: "Сохранить",
            apply: "Применить",
            close: "Закрыть",
            duplicatePath: "Подключение с таким путём уже есть в списке!",
            beerTaps: "Синхронизация пивных кранов",
            connections: "Подключения к Frontol main.gdb"
        };
    }

    loadConfig(config) {
        const settings = config?.connectedFrontolSettings ?? {};
        this.settings = {
            syncBeerTapsSettings: {
                syncBeerTapsEnabled: settings.syncBeerTapsSettings?.syncBeerTapsEnabled ?? false,
                syncBeerTapsPeriodSeconds: settings.syncBeerTapsSettings?.syncBeerTapsPeriodSeconds ?? 30
            },
            printGroupSourseId: settings.printGroupSourseId ?? 0,
            connectionSettings: (settings.connectionSettings ?? []).map(item => ({ ...item }))
        };

        return this;
    }

    render() {
        return {
            id: this.id,
            rows: [
                this._createHiddenFields(),

                {
                    padding: padding,

                    rows: [

                        Label("lConnectedFrontol", this.LABELS.title),

                        {
                            cols: [
                                {
                                    view: "button",
                                    value: this.LABELS.openSettings,
                                    autowidth: false,
                                    width: 180,
                                    click: () => this._showSettingsModal()
                                },
                                {}
                            ]
                        },
                    ]
                }
            ]
        };
    }

    /// Скрытые поля для привязки к основной форме настроек.
    _createHiddenFields() {
        return {
            hidden: true,
            height: 0,
            rows: [
                CheckBox(
                    this.LABELS.syncBeerTaps,
                    "connectedFrontolSettings.syncBeerTapsSettings.syncBeerTapsEnabled",
                    { value: this.settings.syncBeerTapsSettings.syncBeerTapsEnabled }
                ),
                Number(
                    this.LABELS.syncPeriod,
                    "connectedFrontolSettings.syncBeerTapsSettings.syncBeerTapsPeriodSeconds",
                    this.settings.syncBeerTapsSettings.syncBeerTapsPeriodSeconds
                ),
                {
                    view: "text",
                    type: "number",
                    name: "connectedFrontolSettings.printGroupSourseId",
                    id: "hiddenPrintGroupSourceId",
                    value: this.settings.printGroupSourseId
                },
                {
                    view: "formtable",
                    id: this.hiddenTableId,
                    name: "connectedFrontolSettings.connectionSettings",
                    hidden: true,
                    height: 0,
                    data: this.settings.connectionSettings,
                    columns: [
                        { id: "id", header: "id" },
                        { id: "name", header: "name" },
                        { id: "path", header: "path" },
                        { id: "userName", header: "userName" },
                        { id: "password", header: "password" }
                    ]
                }
            ]
        };
    }

    /// Открывает модальное окно настроек подключений Frontol.
    _showSettingsModal() {
        if ($$(this.modalId))
            $$(this.modalId).close();

        const settings = this._readSettingsFromMainForm();

        webix.ui({
            view: "window",
            id: this.modalId,
            position: "center",
            modal: true,
            move: true,
            resize: true,
            width: Math.min(window.innerWidth * 0.9, 1100),
            height: Math.min(window.innerHeight * 0.85, 700),
            head: this._createModalHeader(),
            body: this._createModalBody(settings)
        }).show();

        this._fillModal(settings);
    }

    _createModalHeader() {
        return {
            view: "toolbar",
            elements: [
                { view: "label", label: this.LABELS.modalTitle },
                {
                    view: "icon",
                    icon: "wxi-close",
                    click: () => $$(this.modalId).close()
                }
            ]
        };
    }

    _createModalBody(settings) {
        const toolbar = TableToolbar(this.tableId);
        toolbar.cols.splice(toolbar.cols.length - 1, 0, {
            view: "button",
            value: this.LABELS.importFromAdmin,
            id: "importFrontolAdmin_FrontolConnections",
            autowidth: false,
            width: 320,
            click: () => this._importFromAdmin()
        });

        return {
            padding: 10,
            rows: [
                {
                    view: "richselect",
                    id: this.printGroupSelectId,
                    label: this.LABELS.printGroupSource,
                    labelPosition: "top",
                    placeholder: "Выберите подключение",
                    options: []
                },


                Label("lBeerTaps", this.LABELS.beerTaps),

                {
                    padding: padding,
                    rows: [
                        CheckBox(this.LABELS.syncBeerTaps, "modalSyncBeerTapsEnabled", {
                            id: "modalSyncBeerTapsEnabled",
                            value: settings.syncBeerTapsSettings.syncBeerTapsEnabled,
                            on: {
                                onChange: (enabled) => {
                                    const periodControl = $$("modalSyncBeerTapsPeriodSeconds");
                                    if (periodControl)
                                        enabled ? periodControl.enable() : periodControl.disable();
                                }
                            }
                        }),
                        
                        Number(
                            this.LABELS.syncPeriod,
                            "modalSyncBeerTapsPeriodSeconds",
                            settings.syncBeerTapsSettings.syncBeerTapsPeriodSeconds,
                            "1111",
                            {
                                id: "modalSyncBeerTapsPeriodSeconds",
                                disabled: !settings.syncBeerTapsSettings.syncBeerTapsEnabled
                            }
                        ),
                    ]
                },

                Label("lConnections", this.LABELS.connections),
                
                toolbar,
                
                this._createConnectionsTable(),
                {
                    padding: { top: 10 },
                    cols: [
                        {},

                        {
                            view: "button",
                            value: this.LABELS.apply,
                            autowidth: false,
                            width: 300,
                            click: () => {this._applyAndSave(); $$(this.modalId).close();}
                        },

                        {
                            view: "button",
                            value: this.LABELS.close,
                            autowidth: false,
                            width: 300,
                            click: () => $$(this.modalId).close()
                        },
                    ]
                }
            ]
        };
    }

    _createConnectionsTable() {
        return {
            view: "formtable",
            id: this.tableId,
            data: [],
            resizeColumn: true,
            resizeRow: true,
            select: true,
            minHeight: 250,
            columns: [
                {
                    id: "passwordWarning",
                    header: "",
                    width: 30,
                    template: (obj) => this._isPasswordMissing(obj)
                        ? '<span style="color:red;font-weight:bold;">!</span>'
                        : ""
                },
                { id: "id", header: "Код", hidden: true },
                { id: "name", header: "Наименование", fillspace: true },
                { id: "path", header: "Путь", fillspace: 2 },
                { id: "userName", header: "Пользователь", fillspace: true, hidden: true }
            ],
            on: {
                onAfterSelect: () => {
                    $$(`delete_${this.tableId}`).enable();
                },
                onAfterDelete: (deletedId) => {
                    $$(`delete_${this.tableId}`).disable();
                    const table = $$(this.tableId);
                    if (table.count() === 0)
                        $$(`deleteAll_${this.tableId}`).disable();

                    this._clearPrintGroupIfDeleted(deletedId);
                    this._refreshPrintGroupOptions();
                },
                onBeforeAdd: (_id, obj) => {
                    if (obj.path === undefined) {
                        this._showEditForm(this.LABELS.newConnection);
                        return false;
                    }
                },
                onItemDblClick: (id) => {
                    this._showEditForm(this.LABELS.editConnection, id);
                }
            }
        };
    }

    _showEditForm(title, id) {
        if ($$(this.editFormId))
            $$(this.editFormId).close();

        webix.ui({
            view: "window",
            id: this.editFormId,
            position: "center",
            modal: true,
            move: false,
            resize: false,
            width: Math.min(window.innerWidth * 0.8, 800),
            head: {
                view: "toolbar",
                elements: [
                    { view: "label", label: title },
                    {
                        view: "icon",
                        icon: "wxi-close",
                        click: () => $$(this.editFormId).close()
                    }
                ]
            },
            body: {
                padding: 10,
                rows: [
                    Text(this.LABELS.name, "FrontolConnectionName"),
                    Text(this.LABELS.path, "FrontolConnectionPath", "", frontolDbValidation),
                    Text(this.LABELS.user, "FrontolConnectionUserName", "SYSDBA", { placeholder: "SYSDBA" }),
                    PasswordBox(this.LABELS.password, "FrontolConnectionPassword"),
                    {
                        padding: { top: 10 },
                        cols: [
                            {},
                            {
                                view: "button",
                                value: this.LABELS.save,
                                autowidth: false,
                                width: 300,
                                click: () => this._saveConnection(id)
                            },
                            {
                                view: "button",
                                value: this.LABELS.close,
                                autowidth: false,
                                width: 300,
                                click: () => $$(this.editFormId).close()
                            },
                        ]
                    }
                ]
            }
        }).show();

        this._initEditForm(id);
    }

    _initEditForm(id) {
        const table = $$(this.tableId);

        if (id === undefined) {
            $$("FrontolConnectionUserName").setValue("SYSDBA");
            return;
        }

        const item = table.getItem(id);
        $$("FrontolConnectionName").setValue(item.name ?? "");
        $$("FrontolConnectionPath").setValue(item.path ?? "");
        $$("FrontolConnectionUserName").setValue(item.userName ?? "SYSDBA");
        $$("FrontolConnectionPassword").setValue(item.password ?? "");
    }

    _saveConnection(rowId) {
        const path = $$("FrontolConnectionPath").getValue()?.trim();
        if (!path)
            return;

        const table = $$(this.tableId);
        const duplicates = table.find(row =>
            row.path?.toLowerCase() === path.toLowerCase() &&
            (rowId === undefined || String(row.id) !== String(rowId))
        );

        if (duplicates.length > 0) {
            webix.message({ text: this.LABELS.duplicatePath, type: "error" });
            return;
        }

        const existingItem = rowId !== undefined ? table.getItem(rowId) : null;
        const connection = {
            id: parseInt(existingItem?.id ?? this._nextConnectionId(table), 10) || 0,
            name: $$("FrontolConnectionName").getValue()?.trim() ?? "",
            path,
            userName: $$("FrontolConnectionUserName").getValue()?.trim() || "SYSDBA",
            password: $$("FrontolConnectionPassword").getValue() ?? ""
        };

        if (rowId === undefined)
            table.add(connection);
        else
            table.updateItem(rowId, connection);

        if (table.count() > 0)
            $$(`deleteAll_${this.tableId}`).enable();

        this._refreshPrintGroupOptions();
        $$(this.editFormId).close();
    }

    async _importFromAdmin() {
        try {
            const imported = await importFromFrontolAdmin();
            const table = $$(this.tableId);

            imported.forEach(item => {
                const existing = table.find(row =>
                    row.path?.toLowerCase() === item.path?.toLowerCase()
                );

                if (existing.length > 0) {
                    table.updateItem(existing[0].id, {
                        ...existing[0],
                        name: item.name,
                        userName: item.userName,
                        password: item.password
                    });
                    return;
                }

                table.add({
                    id: item.id ?? this._nextConnectionId(table),
                    name: item.name ?? "",
                    path: item.path ?? "",
                    userName: item.userName ?? "SYSDBA",
                    password: item.password ?? ""
                });
            });

            if (table.count() > 0)
                $$(`deleteAll_${this.tableId}`).enable();

            this._refreshPrintGroupOptions();
            webix.message({ type: "success", text: "Импорт выполнен" });
        } catch (error) {
            webix.message({ type: "error", text: error.message ?? "Ошибка импорта" });
        }
    }

    _applyAndSave() {
        const settings = this._collectModalSettings();
        this._writeSettingsToMainForm(settings);
        saveConfiguration(this.mainFormId);
    }

    _fillModal(settings) {
        $$("modalSyncBeerTapsEnabled").setValue(settings.syncBeerTapsSettings.syncBeerTapsEnabled);
        $$("modalSyncBeerTapsPeriodSeconds").setValue(settings.syncBeerTapsSettings.syncBeerTapsPeriodSeconds);

        const table = $$(this.tableId);
        table.clearAll();
        settings.connectionSettings.forEach(item => table.add(webix.copy(item)));

        if (table.count() > 0)
            $$(`deleteAll_${this.tableId}`).enable();
        else
            $$(`deleteAll_${this.tableId}`).disable();

        $$(`delete_${this.tableId}`).disable();
        this._refreshPrintGroupOptions(settings.printGroupSourseId);
    }

    _collectModalSettings() {
        const table = $$(this.tableId);
        const connectionSettings = [];

        table.data.each(item => {
            connectionSettings.push({
                id: parseInt(item.id, 10) || 0,
                name: item.name ?? "",
                path: item.path ?? "",
                userName: item.userName ?? "",
                password: item.password ?? ""
            });
        });

        return {
            syncBeerTapsSettings: {
                syncBeerTapsEnabled: $$("modalSyncBeerTapsEnabled").getValue(),
                syncBeerTapsPeriodSeconds: +$$("modalSyncBeerTapsPeriodSeconds").getValue() || 30
            },
            printGroupSourseId: +$$(this.printGroupSelectId).getValue() || 0,
            connectionSettings
        };
    }

    _readSettingsFromMainForm() {
        const formValues = $$(this.mainFormId)?.getValues() ?? {};
        const settings = formValues.connectedFrontolSettings;

        if (settings)
            return {
                syncBeerTapsSettings: {
                    syncBeerTapsEnabled: settings.syncBeerTapsSettings?.syncBeerTapsEnabled ?? false,
                    syncBeerTapsPeriodSeconds: settings.syncBeerTapsSettings?.syncBeerTapsPeriodSeconds ?? 30
                },
                printGroupSourseId: settings.printGroupSourseId ?? 0,
                connectionSettings: (settings.connectionSettings ?? []).map(item => ({ ...item }))
            };

        return {
            syncBeerTapsSettings: { ...this.settings.syncBeerTapsSettings },
            printGroupSourseId: this.settings.printGroupSourseId,
            connectionSettings: this.settings.connectionSettings.map(item => ({ ...item }))
        };
    }

    _writeSettingsToMainForm(settings) {
        const form = $$(this.mainFormId);
        const values = form.getValues();
        values.connectedFrontolSettings = settings;
        form.setValues(values, true);

        const hiddenTable = $$(this.hiddenTableId);
        hiddenTable.clearAll();
        settings.connectionSettings.forEach(item => hiddenTable.add(webix.copy(item)));
    }

    _refreshPrintGroupOptions(selectedId) {
        const select = $$(this.printGroupSelectId);
        const table = $$(this.tableId);

        if (!select || !table)
            return;

        const options = [];
        table.data.each(item => {
            options.push({
                id: item.id,
                value: `${item.id} — ${item.name || item.path}`
            });
        });

        const popup = select.getPopup();
        popup.getList().clearAll();
        popup.getList().parse(options);

        const currentId = selectedId ?? select.getValue();
        const exists = options.some(option => +option.id === +currentId);

        if (exists)
            select.setValue(currentId);
        else
            select.setValue("");
    }

    _clearPrintGroupIfDeleted(deletedId) {
        const select = $$(this.printGroupSelectId);
        if (!select)
            return;

        if (+select.getValue() === +deletedId)
            select.setValue("");
    }

    _nextConnectionId(table) {
        let maxId = -1;
        table.data.each(item => {
            if (+item.id > maxId)
                maxId = +item.id;
        });
        return maxId + 1;
    }

    _isPasswordMissing(row) {
        return !row.password?.trim();
    }
}

export default function (id, config) {
    return new ConnectedFrontolConfigurationElement(id)
        .loadConfig(config)
        .render();
}
