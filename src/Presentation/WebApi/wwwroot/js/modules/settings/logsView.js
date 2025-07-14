import { ApiServerAdres } from '../../utils/net.js';

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
    }

    render() {
        $$("toolbarLabel").setValue("FMU-API: Логи работы");

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
            id: "logFiles",
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
            id: "refreshLogBtn",
            autowidth: "false",
            width: 400,
            click: async _ => {
                var combo = $$("logFiles");
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
            id: "uploadLogBtn",
            autowidth: "false",
            width: 400,
            click: async _ => {
                var logText = $$("log");
                var fileHandler = await this._getNewFileHandle();
                await this._writeTextFile(fileHandler, logText.getValue());
            }
        }
    }

    _logTextArea() {
        return {
            view: "textarea",
            id: "log",
            readonly: true,
        }
    }

    _loadLogs(fileName, elementId) {
        $$(elementId).disable();

        fileName = fileName.length == 0 ? "now" : fileName;

        webix.ajax().get(ApiServerAdres(`/configuration/logs/${fileName}`))
            .then(function (data) {
                var logInfo = data.json();

                var combo = $$("logFiles");

                combo.blockEvent();

                var comboOptions = combo.getPopup().getList();
                comboOptions.clearAll();
                comboOptions.parse(logInfo.fileNames);

                combo.setValue(logInfo.selectedFile);

                combo.unblockEvent();

                var logText = $$("log");
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