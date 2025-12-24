
import { ApiServerAddress } from '../../utils/net.js';

class MarkCheckView {
    constructor(id) {
        this.formName = "MarkCheckView";
        this.id = id;
        this.apiAddress = "/api/fmu/document";

        this.LABELS = {
            formTitle: "FMU-API: Проверка маркировки",
            innLabel: "ИНН организации",
            innPlaceholder: "Введите ИНН",
            markLabel: "Штрихкод маркировки",
            markPlaceholder: "Введите или вставьте штрихкод маркировки",
            checkButton: "Проверить",
            jsonResponseLabel: "JSON ответ",
            decodedResponseLabel: "Расшифровка ответа",
            noResponse: "Нет данных"
        };

        this.NAMES = {
            innInput: "innInput",
            markInput: "markInput",
            checkButton: "checkButton",
            jsonResponse: "jsonResponse",
            decodedResponse: "decodedResponse"
        };

        this.lastResponse = null;
    }

    loadConfig() {
        return this;
    }

    render() {
        $$("toolbarLabel").setValue(this.LABELS.formTitle);

        const formElements = [
            {
                view: "form",
                borderless: true,
                elements: [
                    {
                        view: "text",
                        id: this.NAMES.innInput,
                        label: this.LABELS.innLabel,
                        labelWidth: 150,
                        value: "246605656953",
                        placeholder: this.LABELS.innPlaceholder
                    },
                    {
                        view: "textarea",
                        id: this.NAMES.markInput,
                        label: this.LABELS.markLabel,
                        labelWidth: 150,
                        placeholder: this.LABELS.markPlaceholder,
                        height: 100,
                        value: ""
                    },
                    {
                        view: "button",
                        id: this.NAMES.checkButton,
                        value: this.LABELS.checkButton,
                        width: 150,
                        click: () => this._onCheck()
                    }
                ]
            },
            {
                cols: [
                    {
                        view: "form",
                        borderless: true,
                        elements: [
                            {
                                view: "label",
                                label: this.LABELS.jsonResponseLabel,
                                labelAlign: "center"
                            },
                            {
                                view: "textarea",
                                id: this.NAMES.jsonResponse,
                                readonly: true,
                                fillspace: true,
                                value: this.LABELS.noResponse
                            }
                        ]
                    },
                    { view: "resizer" },
                    {
                        view: "form",
                        borderless: true,
                        elements: [
                            {
                                view: "label",
                                label: this.LABELS.decodedResponseLabel,
                                labelAlign: "center"
                            },
                            {
                                view: "textarea",
                                id: this.NAMES.decodedResponse,
                                readonly: true,
                                fillspace: true,
                                value: this.LABELS.noResponse
                            }
                        ]
                    }
                ]
            },
            {}
        ];

        const form = {
            view: "form",
            id: this.id,
            name: this.formName,
            elements: formElements
        };

        return form;
    }

    // Преобразование строки в base64 (UTF-8)
    _encodeToBase64(str) {
        try {
            // Используем TextEncoder для правильной обработки UTF-8, включая символы GS
            const utf8Bytes = new TextEncoder().encode(str);
            // Преобразуем Uint8Array в строку для btoa
            let binary = '';
            utf8Bytes.forEach(byte => {
                binary += String.fromCharCode(byte);
            });
            return btoa(binary);
        } catch (error) {
            console.error("Ошибка кодирования в base64:", error);
            throw error;
        }
    }

    // Формирование запроса
    _buildRequest(inn, markingCode) {
        const base64Mark = this._encodeToBase64(markingCode);
        
        return {
            positions: [
                {
                    organisation: {
                        inn: inn
                    },
                    marking_codes: [base64Mark]
                }
            ],
            action: "check",
            type: "refund_receipt"
        };
    }

    // Отправка запроса на сервер
    async _onCheck() {
        const innInput = $$(this.NAMES.innInput);
        const markInput = $$(this.NAMES.markInput);
        const checkButton = $$(this.NAMES.checkButton);
        const form = $$(this.id);

        if (!innInput || !markInput) {
            webix.message({ text: "Ошибка: не найдены поля ввода", type: "error" });
            return;
        }

        const inn = innInput.getValue().trim();
        const markingCode = markInput.getValue().trim();

        if (!inn) {
            webix.message({ text: "Введите ИНН организации", type: "error" });
            return;
        }

        if (!markingCode) {
            webix.message({ text: "Введите штрихкод маркировки", type: "error" });
            return;
        }

        // Отключаем форму и показываем прогресс
        checkButton.disable();
        webix.extend(form, webix.ProgressBar);
        form.showProgress({ type: "icon" });

        try {
            const requestData = this._buildRequest(inn, markingCode);
            const apiUrl = ApiServerAddress(this.apiAddress);

            if (!apiUrl) {
                throw new Error("Не настроен адрес сервера API");
            }

            const response = await fetch(apiUrl, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(requestData)
            });

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(`Ошибка сервера ${response.status}: ${errorText}`);
            }

            const responseData = await response.json();
            this.lastResponse = responseData;
            this._displayResponse(responseData);

        } catch (error) {
            console.error("Ошибка при проверке маркировки:", error);
            webix.message({ 
                text: `Ошибка при проверке маркировки: ${error.message}`, 
                type: "error" 
            });
            this._clearResponse();
        } finally {
            checkButton.enable();
            form.hideProgress();
        }
    }

    // Отображение ответа
    _displayResponse(responseData) {
        // Отображение JSON
        const jsonResponse = $$(this.NAMES.jsonResponse);
        if (jsonResponse) {
            jsonResponse.setValue(JSON.stringify(responseData, null, 2));
        }

        // Отображение расшифровки
        const decodedResponse = $$(this.NAMES.decodedResponse);
        if (decodedResponse) {
            decodedResponse.setValue(this._decodeResponse(responseData));
        }
    }

    // Расшифровка ответа в удобочитаемый вид
    _decodeResponse(response) {
        let result = "";

        // Основные поля
        result += `Код ответа: ${response.code || 0}\n`;
        if (response.error) {
            result += `Ошибка: ${response.error}\n`;
        }
        result += "\n";

        // Stamps и Marking_codes
        if (response.stamps && response.stamps.length > 0) {
            result += `Stamps (${response.stamps.length}):\n`;
            response.stamps.forEach((stamp, index) => {
                result += `  ${index + 1}. ${stamp}\n`;
            });
            result += "\n";
        }

        if (response.marking_codes && response.marking_codes.length > 0) {
            result += `Marking codes (${response.marking_codes.length}):\n`;
            response.marking_codes.forEach((code, index) => {
                result += `  ${index + 1}. ${code}\n`;
            });
            result += "\n";
        }

        // Truemark response
        if (response.truemark_response) {
            result += "=== Truemark Response ===\n";
            const tr = response.truemark_response;
            result += `Код: ${tr.code || 0}\n`;
            if (tr.description) {
                result += `Описание: ${tr.description}\n`;
            }
            if (tr.reqId) {
                result += `ReqId: ${tr.reqId}\n`;
            }
            if (tr.reqTimestamp) {
                const date = new Date(tr.reqTimestamp);
                result += `Время запроса: ${date.toLocaleString()}\n`;
            }
            if (tr.version) {
                result += `Версия: ${tr.version}\n`;
            }
            if (tr.codes && tr.codes.length > 0) {
                result += `Коды (${tr.codes.length}):\n`;
                tr.codes.forEach((code, index) => {
                    result += `  Код ${index + 1}:\n`;
                    if (code.cis) result += `    CIS: ${code.cis}\n`;
                    if (code.gtin) result += `    GTIN: ${code.gtin}\n`;
                    if (code.serial) result += `    Serial: ${code.serial}\n`;
                    if (code.status !== undefined) result += `    Status: ${code.status}\n`;
                    if (code.sold !== undefined) result += `    Sold: ${code.sold}\n`;
                    if (code.isExpired !== undefined) result += `    IsExpired: ${code.isExpired}\n`;
                    if (code.realizable !== undefined) result += `    Realizable: ${code.realizable}\n`;
                    if (code.printView) result += `    PrintView: ${code.printView}\n`;
                });
            }
            result += "\n";
        }

        // Truemark responses
        if (response.truemark_responses && response.truemark_responses.length > 0) {
            result += `=== Truemark Responses (${response.truemark_responses.length}) ===\n`;
            response.truemark_responses.forEach((trResp, index) => {
                result += `Ответ ${index + 1}:\n`;
                if (trResp.inn) result += `  INN: ${trResp.inn}\n`;
                if (trResp.kpp) result += `  KPP: ${trResp.kpp}\n`;
                if (trResp.response) {
                    const resp = trResp.response;
                    result += `  Код: ${resp.code || 0}\n`;
                    if (resp.description) result += `  Описание: ${resp.description}\n`;
                    if (resp.codes && resp.codes.length > 0) {
                        result += `  Коды: ${resp.codes.length}\n`;
                    }
                }
                result += "\n";
            });
        }

        // Offline truemark responses
        if (response.offline_truemark_response && response.offline_truemark_response.length > 0) {
            result += `=== Offline Truemark Responses (${response.offline_truemark_response.length}) ===\n`;
            response.offline_truemark_response.forEach((resp, index) => {
                result += `Ответ ${index + 1}: Код ${resp.code || 0}\n`;
            });
            result += "\n";
        }

        // ESM response
        if (response.esm_response) {
            result += "=== ESM Response ===\n";
            const esm = response.esm_response;
            if (esm.code !== undefined) result += `Код: ${esm.code}\n`;
            if (esm.message) result += `Сообщение: ${esm.message}\n`;
            result += "\n";
        }

        // DMDK responses
        if (response.dmdk_responses && response.dmdk_responses.length > 0) {
            result += `=== DMDK Responses (${response.dmdk_responses.length}) ===\n`;
            response.dmdk_responses.forEach((resp, index) => {
                result += `Ответ ${index + 1}: Код ${resp.code || 0}\n`;
            });
            result += "\n";
        }

        // Флаги и метаданные
        result += "=== Метаданные ===\n";
        if (response["fmu-api-offline"] !== undefined) {
            result += `Offline режим: ${response["fmu-api-offline"]}\n`;
        }
        if (response["fmu-api-local-Module"] !== undefined) {
            result += `Локальный модуль: ${response["fmu-api-local-Module"]}\n`;
        }
        if (response["fmu-api-print-group"] !== undefined) {
            result += `Print Group Code: ${response["fmu-api-print-group"]}\n`;
        }
        if (response["fmu-api-version"]) {
            result += `Версия FMU-API: ${response["fmu-api-version"]}\n`;
        }

        return result || this.LABELS.noResponse;
    }

    // Очистка ответа
    _clearResponse() {
        const jsonResponse = $$(this.NAMES.jsonResponse);
        const decodedResponse = $$(this.NAMES.decodedResponse);

        if (jsonResponse) {
            jsonResponse.setValue(this.LABELS.noResponse);
        }
        if (decodedResponse) {
            decodedResponse.setValue(this.LABELS.noResponse);
        }
    }
}

export default function (id) {
    const view = new MarkCheckView(id)
        .loadConfig()
        .render();

    return view;
}

