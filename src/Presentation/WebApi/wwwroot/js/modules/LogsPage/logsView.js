import {ApiServerAddress} from '../../utils/net.js';

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

        let formElements = [this._logToolbar(), this._logTextArea()];

        return {
            view: "form",
            id: this.id,
            name: this.formName,
            disabled: true,
            elements: formElements
        };
    }

    delayedDataLoading() {
        setTimeout(() => {
            this._loadLogs();
        }, 100);

        return this;
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
                const combo = $$(this.NAMES.logFiles);
                const chosenLogName = combo.getValue();

                if (!chosenLogName)
                    return;

                if (chosenLogName === "")
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
            on: {
                //"onKeyPress": (code, event) => this._handleKeyPress(code, event), 
            }
        }
    }

    _loadLogs(fileName = "") {
        $$(this.id).disable();

        fileName = fileName.length === 0 ? "now" : fileName;
        

        webix.ajax().get(ApiServerAddress(`/configuration/logs/${fileName}`))
            .then( (data) => {
                let logInfo = data.json();

                const combo = $$(this.NAMES.logFiles);

                combo.blockEvent();

                let comboOptions = combo.getPopup().getList();
                comboOptions.clearAll();
                comboOptions.parse(logInfo.fileNames);

                combo.setValue(logInfo.selectedFile);

                combo.unblockEvent();

                let logText = $$(this.NAMES.log);
                const lines = logInfo.log.split('\n').reverse().join('\n');
                logText.setValue(lines);

                $$(this.id).enable();
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

    _handleKeyPress(code, e) {
        //console.log(code);
        // Page Up (33), Page Down (34)
        if (!(code === 33 || code === 34))
            return;
            
        var textarea = $$(this.NAMES.log);
        
        if (!textarea)
            return;
        
        var dom = textarea.getInputNode();
        if (code === 33) {
            dom.scrollTop = Math.max(0, dom.scrollTop - dom.clientHeight);
        } else {
            dom.scrollTop = Math.min(dom.scrollHeight - dom.clientHeight, dom.scrollTop + dom.clientHeight);
        }
        e.preventDefault();
    }
}

export default function (id) {
    const view = new LogsView(id)
        .delayedDataLoading()
        .render();

    return view;
}