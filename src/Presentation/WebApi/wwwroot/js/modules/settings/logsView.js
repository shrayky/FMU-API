import { ApiServerAddress } from '../../utils/net.js';

class LogsView {
    constructor(id) {
        this.formName = "LogsView";
        this.id = id;

        this.LABELS = {
            formTitle: "FMU-API: Логи работы",
            title: "Просмотр логов",
            logFile: "Лог работы:",
            refresh: "Обновить",
            save: "Сохранить",
            downloadAll: "Выгрузить все логи",
            close: "Закрыть",
            fileDescription: "Текстовый файл",
            errorDownload: "Ошибка при выгрузке логов",
            errorLoad: "Ошибка при загрузке логов"
        };

        this.NAMES = {
            toolbarLabel: "toolbarLabel",
            logFiles: "logFiles",
            refreshLogBtn: "refreshLogBtn",
            uploadLogBtn: "uploadLogBtn",
            log: "log"
        };
    }

    render() {
        $$(this.NAMES.toolbarLabel).setValue(this.LABELS.formTitle);

        var formElements = [this._logToolbar(), this._logTextArea()];

        var form = {
            view: "form",
            id: this.id,
            name: this.formName,
            disabled: true,
            on: {
                onViewShow: this._loadLogs("", this.id),
            },
            elements: formElements
        }

        return form;
    }

    _logToolbar() {
        return {
            cols: [
                this._logsCombo(),
                this._refreshButton(),
                this._uploadLogButton()
            ]
        }
    }

    _logsCombo() {
        return {
            view: "combo",
            id: this.NAMES.logFiles,
            label: this.LABELS.logFile,
            labelWidth: 100,
            newvalues: false,
            options: [],
            on: {
                "onChange": (newValue) => {
                    this._loadLogs(newValue, this.id);
                }
            }
        }
    }

    _refreshButton() {
        return {
            view: "button",
            value: this.LABELS.refresh,
            id: this.NAMES.refreshLogBtn,
            autowidth: "false",
            width: 400,
            click: async _ => {
                var combo = $$(this.NAMES.logFiles);
                var chosenLogName = combo.getValue();

                if (!chosenLogName)
                    return;

                if (chosenLogName == "")
                    return;

                this._loadLogs(chosenLogName, this.id);
            }
        }
    }

    _uploadLogButton() {
        return {
            view: "button",
            value: this.LABELS.save,
            id: this.NAMES.uploadLogBtn,
            autowidth: "false",
            width: 400,
            click: async _ => {
                var logText = $$(this.NAMES.log);
                var fileHandler = await this._getNewFileHandle();
                await this._writeTextFile(fileHandler, logText.getValue());
            }
        }
    }

    _logTextArea() {
        return {
            view: "textarea",
            id: this.NAMES.log,
            readonly: true,
        }
    }

    _loadLogs(fileName, elementId) {
        $$(elementId).disable();

        fileName = fileName.length == 0 ? "now" : fileName;
        

        webix.ajax().get(ApiServerAddress(`/configuration/logs/${fileName}`))
            .then( (data) => {
                var logInfo = data.json();

                var combo = $$(this.NAMES.logFiles);

                combo.blockEvent();

                var comboOptions = combo.getPopup().getList();
                comboOptions.clearAll();
                comboOptions.parse(logInfo.fileNames);

                combo.setValue(logInfo.selectedFile);

                combo.unblockEvent();

                var logText = $$(this.NAMES.log);
                const lines = logInfo.log.split('\n').reverse().join('\n');
                logText.setValue(lines);

                $$(elementId).enable();
            });
    }

    async _getNewFileHandle() {
        const options = {
            types: [
                {
                    keepExistingData: false,
                    description: 'Текстовый файл',
                    accept: {
                        'text/plain': ['.txt'],
                    },
                },
            ],
        };
        const handle = await window.showSaveFilePicker(options);
        return handle;
    }

    async _writeTextFile(fileHandle, data) {
        const writableStream = await fileHandle.createWritable();
        const lines = data.split('\n').reverse().join('\n');
        await writableStream.write(lines);
        await writableStream.close();
    }
}

export default function (id) {
    return new LogsView(id)
        .render();
}