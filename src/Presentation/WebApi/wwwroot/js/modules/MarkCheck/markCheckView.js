
import { ServerAdres } from '../../utils/net.js';
import { ScannerWedge } from '../../utils/scannerWedge.js';

class MarkCheckView {
    constructor(id) {
        this.formName = "MarkCheckView";
        this.id = id;
        this.apiAddress = "/api/fmu/document";
        this.trueApiAddress = "/api/ts/cises/info";

        this.LABELS = {
            formTitle: "FMU-API: Проверка маркировки",
            innLabel: "ИНН организации",
            innPlaceholder: "Введите ИНН",
            markLabel: "Штрихкод маркировки",
            markPlaceholder: "Введите или вставьте штрихкод маркировки",
            checkButton: "Проверить",
            jsonResponseLabel: "JSON ответ",
            decodedResponseLabel: "Расшифровка ответа",
            noResponse: "Нет данных",
            permissiveBlockTitle: "=== Разрешительный режим / ТСПИоТ ===",
            trueApiBlockTitle: "=== True API (cises/info) ===",
            trueApiNoData: "Нет данных True API"
        };

        this.NAMES = {
            innInput: "innInput",
            markInput: "markInput",
            checkButton: "checkButton",
            jsonResponse: "jsonResponse",
            decodedResponse: "decodedResponse"
        };

        this.lastResponse = null;
        this.defaultInn = "";

        this.scanner = new ScannerWedge({
            timeoutMs: 50,
            onScan: (code, meta) => this._onScan(code, meta)
        });
    }

    async _loadInnFromConfig() {
        try {
            const apiUrl = ServerAdres('/api/configuration/OrganisationConfig');
            if (!apiUrl) {
                return;
            }

            const response = await fetch(apiUrl);
            if (!response.ok) {
                console.warn("Не удалось загрузить настройки организаций");
                return;
            }

            const orgConfig = await response.json();
            
            if (orgConfig.printGroups && orgConfig.printGroups.length > 0) {
                const firstOrg = orgConfig.printGroups[0];
                if (firstOrg.inn) {
                    this.defaultInn = firstOrg.inn;
                    const innInput = $$(this.NAMES.innInput);
                    if (innInput) {
                        innInput.setValue(this.defaultInn);
                    }
                }
            }
        } catch (error) {
            console.warn("Ошибка при загрузке настроек организаций:", error);
        }
    }

    render() {
        $$("toolbarLabel").setValue(this.LABELS.formTitle);

        const formElements = [
            {
                rows: [
                    {
                        view: "text",
                        id: this.NAMES.innInput,
                        label: this.LABELS.innLabel,
                        labelWidth: 180,
                        placeholder: this.LABELS.innPlaceholder,
                        value: this.defaultInn || ""
                    },

                    {
                        cols: [
                            {
                                view: "text",
                                id: this.NAMES.markInput,
                                label: this.LABELS.markLabel,
                                labelWidth: 180,
                                placeholder: this.LABELS.markPlaceholder,
                                value: ""
                            },
                
                            {
                                view: "button",
                                id: this.NAMES.checkButton,
                                value: this.LABELS.checkButton,
                                width: 150,
                                click: () => this._onCheck()
                            }, 
                        ]
                    }
                ]
            },
            {
                cols: [
                    {
                        rows: [
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
                            },
                        ]
                    },
                    { view: "resizer" },
                    {
                        rows: [
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
                            },
                        ]
                    },
                ]
            },
        ];

        var form = {
            view: "form",
            id: this.id,
            name: this.formName,
            elements: formElements,
            on: {
                onAfterRender: () => {
                    this.scanner.start();
                    setTimeout(() => {
                        const markInput = $$(this.NAMES.markInput);
                        if (markInput) {
                            markInput.focus();
                        }
                    }, 50);
                },
                onDestruct: () => {
                    this.scanner.stop();
                }
            }
        }

        this._formConfig = form;
        return this;
    }

    delayedInnLoading() {
        setTimeout(() => {
            this._loadInnFromConfig();
        }, 10);

        this.scanner.start();

        return this;
    }

    _onScan(code, meta = {}) {
        const markInput = $$(this.NAMES.markInput);
        if (markInput) {
            markInput.setValue(code);
            markInput.focus();
        }

        const warnings = [];
        if (meta.capsLock) {
            warnings.push("Включён Caps Lock — раскладка сканера может исказить код");
        }
        if (meta.cyrillic) {
            warnings.push("В штрихкоде есть русские символы — проверьте раскладку клавиатуры");
        }

        if (warnings.length > 0) {
            webix.message({
                text: warnings.join(". "),
                type: "error",
                expire: 5000
            });
        }
    }

    _encodeToBase64(str) {
        try {
            const utf8Bytes = new TextEncoder().encode(str);
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
            type: "receipt"
        };
    }

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

        checkButton.disable();
        webix.extend(form, webix.ProgressBar);
        form.showProgress({ type: "icon" });

        try {
            const documentUrl = ServerAdres(this.apiAddress);
            const trueApiUrl = ServerAdres(this.trueApiAddress);

            if (!documentUrl || !trueApiUrl) {
                throw new Error("Не настроен адрес сервера API");
            }

            const requestData = this._buildRequest(inn, markingCode);

            const documentPromise = fetch(documentUrl, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(requestData)
            }).then(async (response) => {
                if (!response.ok) {
                    const errorText = await response.text();
                    throw new Error(`Ошибка сервера ${response.status}: ${errorText}`);
                }
                return response.json();
            });

            const trueApiPromise = fetch(trueApiUrl, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ inn: inn, cises: [markingCode] })
            }).then(async (response) => {
                if (!response.ok) {
                    const errorText = await response.text();
                    throw new Error(`Ошибка сервера ${response.status}: ${errorText}`);
                }
                return response.json();
            });

            const [documentResult, trueApiResult] = await Promise.allSettled([
                documentPromise,
                trueApiPromise
            ]);

            const combined = {
                permissive: documentResult.status === "fulfilled"
                    ? documentResult.value
                    : { error: documentResult.reason?.message || String(documentResult.reason) },
                trueApi: trueApiResult.status === "fulfilled"
                    ? trueApiResult.value
                    : { status: "error", reason: trueApiResult.reason?.message || String(trueApiResult.reason) }
            };

            this.lastResponse = combined;

            if (documentResult.status === "rejected" && trueApiResult.status === "rejected") {
                webix.message({
                    text: `Ошибка при проверке маркировки: ${documentResult.reason?.message || "оба источника недоступны"}`,
                    type: "error"
                });
            } else if (documentResult.status === "rejected") {
                webix.message({
                    text: `Ошибка разрешительного режима: ${documentResult.reason?.message || ""}`,
                    type: "error"
                });
            }

            this._displayCombinedResponse(combined);

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

    _displayResponse(responseData) {
        const jsonResponse = $$(this.NAMES.jsonResponse);
        if (jsonResponse) {
            jsonResponse.setValue(JSON.stringify(responseData, null, 2));
        }

        const decodedResponse = $$(this.NAMES.decodedResponse);
        if (decodedResponse) {
            decodedResponse.setValue(this._decodeResponse(responseData));
        }
    }

    _displayCombinedResponse(combined) {
        const jsonResponse = $$(this.NAMES.jsonResponse);
        if (jsonResponse) {
            jsonResponse.setValue(JSON.stringify(combined, null, 2));
        }

        const decodedResponse = $$(this.NAMES.decodedResponse);
        if (decodedResponse) {
            decodedResponse.setValue(this._decodeCombinedResponse(combined));
        }
    }

    _decodeCombinedResponse(combined) {
        let result = "";

        result += `${this.LABELS.permissiveBlockTitle}\n`;
        if (combined.permissive && !combined.permissive.error) {
            result += this._decodeResponse(combined.permissive);
        } else {
            result += `Ошибка: ${combined.permissive?.error || this.LABELS.noResponse}\n`;
        }

        result += "\n";
        result += `${this.LABELS.trueApiBlockTitle}\n`;
        result += this._decodeTrueApiResponse(combined.trueApi);

        return result || this.LABELS.noResponse;
    }

    _decodeTrueApiResponse(trueApi) {
        if (!trueApi) {
            return `${this.LABELS.trueApiNoData}\n`;
        }

        let result = "";
        result += `Статус: ${trueApi.status || "error"}\n`;

        if (trueApi.status !== "ok") {
            if (trueApi.reason) {
                result += `Причина: ${trueApi.reason}\n`;
            }
            return result;
        }

        if (!trueApi.data || trueApi.data.length === 0) {
            result += `${this.LABELS.trueApiNoData}\n`;
            return result;
        }

        trueApi.data.forEach((item, index) => {
            result += `Элемент ${index + 1}:\n`;
            if (item.errorMessage) result += `  ErrorMessage: ${item.errorMessage}\n`;
            if (item.errorCode) result += `  ErrorCode: ${item.errorCode}\n`;

            const info = item.cisInfo;
            if (!info) {
                result += "  CisInfo: нет данных\n";
                return;
            }

            if (info.requestedCis) result += `  RequestedCis: ${info.requestedCis}\n`;
            if (info.cis) result += `  CIS: ${info.cis}\n`;
            if (info.gtin) result += `  GTIN: ${info.gtin}\n`;
            if (info.printView) result += `  PrintView: ${info.printView}\n`;
            if (info.status) result += `  Status: ${info.status}\n`;
            if (info.ownerInn) result += `  OwnerInn: ${info.ownerInn}\n`;
            if (info.ownerName) result += `  OwnerName: ${info.ownerName}\n`;
            if (info.producerInn) result += `  ProducerInn: ${info.producerInn}\n`;
            if (info.productGroup) result += `  ProductGroup: ${info.productGroup}\n`;
            if (info.productGroupId !== undefined && info.productGroupId !== null) {
                result += `  ProductGroupId: ${info.productGroupId}\n`;
            }
            if (info.expirationDate) result += `  ExpirationDate: ${info.expirationDate}\n`;
            if (info.expireDate) result += `  ExpireDate: ${info.expireDate}\n`;
            if (info.markWithdraw !== undefined) result += `  MarkWithdraw: ${info.markWithdraw}\n`;
            if (info.packageType) result += `  PackageType: ${info.packageType}\n`;
        });

        return result;
    }

    _decodeResponse(response) {
        let result = "";

        result += `Код ответа: ${response.code || 0}\n`;
        if (response.error) {
            result += `Ошибка: ${response.error}\n`;
        }
        result += "\n";

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

        if (response.offline_truemark_response && response.offline_truemark_response.length > 0) {
            result += `=== Offline Truemark Responses (${response.offline_truemark_response.length}) ===\n`;
            response.offline_truemark_response.forEach((resp, index) => {
                result += `Ответ ${index + 1}: Код ${resp.code || 0}\n`;
            });
            result += "\n";
        }

        if (response.esm_response) {
            result += "=== ESM Response ===\n";
            const esm = response.esm_response;
            if (esm.code !== undefined) result += `Код: ${esm.code}\n`;
            if (esm.message) result += `Сообщение: ${esm.message}\n`;
            result += "\n";
        }

        if (response.dmdk_responses && response.dmdk_responses.length > 0) {
            result += `=== DMDK Responses (${response.dmdk_responses.length}) ===\n`;
            response.dmdk_responses.forEach((resp, index) => {
                result += `Ответ ${index + 1}: Код ${resp.code || 0}\n`;
            });
            result += "\n";
        }

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
    const view = new MarkCheckView(id);
    view.render();
    view.delayedInnLoading();

    return view._formConfig;
}

