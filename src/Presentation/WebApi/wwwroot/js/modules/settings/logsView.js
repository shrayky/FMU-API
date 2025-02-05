import { Label } from "../../utils/ui.js";
import { ApiServerAdres } from '../../utils/net.js';

export default function logsView(id) {
    $$("toolbarLabel").setValue("FMU-API: Логи работы");

    return {
        view: "form",
        id: id,
        name: "fmuApiLogs",
        disabled: true,
        on:{
            onViewShow: loadLogs("", id),
        },
        elements: [
            {
                cols: [
                    {
                        view: "combo",
                        id: "logFiles",
                        label: "Лог работы:",
                        labelWidth: 100,
                        newvalues: false,
                        options: [],
                        on: {
                            "onChange": (newValue, oldValue, cfg) => {
                                loadLogs(newValue, id);
                            }
                        }
                    },

                    {
                        view: "button",
                        value: "Обновить",
                        id: "refreshLogBtn",
                        autowidth: "false",
                        width: 400,
                        click: async _ => {
                            var combo = $$("logFiles");
                            var chosenLogFile = combo.getValue();

                            if (!chosenLogFile)
                                return;

                            if (chosenLogFile == "")
                                return;

                            loadLogs(chosenLogFile, id);
                        }

                    },

                    {
                        view: "button",
                        value: "Сохранить",
                        id: "saveLogBtn",
                        autowidth: "false",
                        width: 400,
                        click: async _ => {
                            var logText = $$("log");
                            var fileHandler = await getNewFileHandle();
                            await writeTextFile(fileHandler, logText.getValue());
                        }
                    }

                ]
            },

            {
                view: "textarea",
                id: "log",
                readonly: true,
            }

        ],
    }
}

function loadLogs(fileName, elementId) {
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
            logText.setValue(logInfo.log);

            $$(elementId).enable();
        });
}

async function getNewFileHandle() {
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

async function writeTextFile(fileHandle, data) {
    const writableStream = await fileHandle.createWritable();
    await writableStream.write(data);
    await writableStream.close();
}