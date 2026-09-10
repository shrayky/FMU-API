
import { ServerAdres } from '../../utils/net.js';
import { ScannerWedge } from '../../utils/scannerWedge.js';
import { ClipboardMarkWatcher } from '../../utils/clipboardMarkWatcher.js';
import { isMobileDevice } from '../../utils/device.js';
import { buildMarkCardsHtml, buildCheckingHtml, buildScanHintHtml, ensureMarkCheckStyles } from './markCheckCards.js';

class MarkCheckView {
    constructor(id) {
        this.formName = "MarkCheckView";
        this.id = id;
        this.apiAddress = "/api/fmu/document";
        this.trueApiAddress = "/api/ts/cises/info";
        this.isMobile = isMobileDevice();

        this.LABELS = {
            formTitle: this.isMobile ? "Проверка марки" : "FMU-API: Проверка маркировки",
            innLabel: "ИНН организации",
            innPlaceholder: "Введите ИНН",
            markLabel: "Штрихкод маркировки",
            markPlaceholder: "Введите или вставьте штрихкод маркировки",
            scanHint: "Сканируйте марку",
            checkButton: "Проверить",
            checking: "Проверка…",
            noResponse: "Нет данных",
            showJson: "Показать JSON",
            hideJson: "Скрыть JSON"
        };

        this.NAMES = {
            innInput: "innInput",
            markInput: "markInput",
            checkButton: "checkButton",
            markCards: "markCards",
            jsonToggle: "jsonToggle",
            jsonResponse: "jsonResponse"
        };

        this.lastResponse = null;
        this.defaultInn = "";
        this._checking = false;
        this._autoCheckTimer = null;
        this._idleCheckMs = this.isMobile ? 400 : 300;
        this._rawMark = "";
        this._applyingScan = false;

        this.scanner = new ScannerWedge({
            timeoutMs: this.isMobile ? 250 : 50,
            completeOnIdle: this.isMobile,
            gsAsEnterHoldMs: this.isMobile ? 250 : 0,
            onBufferChange: (code) => this._onBuffer(code),
            onScan: (code, meta) => this._onScan(code, meta)
        });

        this.clipboardWatcher = new ClipboardMarkWatcher({
            intervalMs: 350,
            onCode: (code) => this._onClipboardMark(code)
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
        if (!this.isMobile)
            $$("toolbarLabel").setValue(this.LABELS.formTitle);

        ensureMarkCheckStyles();

        const form = {
            view: "form",
            id: this.id,
            name: this.formName,
            padding: this.isMobile ? 4 : undefined,
            elements: [
                this._inputBlock(),
                {
                    view: "scrollview",
                    gravity: 3,
                    body: {
                        view: "template",
                        id: this.NAMES.markCards,
                        borderless: true,
                        autoheight: true,
                        css: "mark-cards",
                        template: this.isMobile ? buildScanHintHtml() : buildMarkCardsHtml(null)
                    }
                },
                this._jsonBlock()
            ],
            on: {
                onAfterRender: () => {
                    this._setMobileToolbarVisible(false);
                    this._bindScanner();
                },
                onDestruct: () => {
                    if (this._autoCheckTimer)
                        clearTimeout(this._autoCheckTimer);

                    this._setMobileToolbarVisible(true);
                    this.clipboardWatcher.stop();
                    this.scanner.stop();
                }
            }
        };

        this._formConfig = form;
        return this;
    }

    _inputBlock() {
        if (this.isMobile) {
            return {
                view: "text",
                id: this.NAMES.innInput,
                hidden: true,
                value: this.defaultInn || ""
            };
        }

        return {
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
                    view: "text",
                    id: this.NAMES.markInput,
                    label: this.LABELS.markLabel,
                    labelWidth: 180,
                    placeholder: this.LABELS.markPlaceholder,
                    value: "",
                    on: this._markInputEvents()
                },
                {
                    cols: [
                        {},
                        {
                            view: "button",
                            id: this.NAMES.checkButton,
                            value: this.LABELS.checkButton,
                            width: 150,
                            click: () => this._onCheck()
                        }
                    ]
                }
            ]
        };
    }

    _jsonBlock() {
        if (this.isMobile) {
            return {
                view: "textarea",
                id: this.NAMES.jsonResponse,
                hidden: true,
                readonly: true
            };
        }

        return {
            rows: [
                {
                    cols: [
                        {
                            view: "button",
                            id: this.NAMES.jsonToggle,
                            value: this.LABELS.showJson,
                            css: "webix_transparent mark-json-link",
                            width: 160,
                            click: () => this._toggleJson()
                        },
                        {}
                    ]
                },
                {
                    view: "textarea",
                    id: this.NAMES.jsonResponse,
                    hidden: true,
                    readonly: true,
                    height: 280,
                    value: this.LABELS.noResponse
                }
            ]
        };
    }

    delayedInnLoading() {
        setTimeout(() => {
            this._loadInnFromConfig();
        }, 10);

        this.scanner.start();

        return this;
    }

    _bindScanner() {
        this.scanner.start();
        setTimeout(() => {
            if (this.isMobile) {
                const capture = document.getElementById("fmuScanCapture");
                this.scanner.start(capture);
                capture?.focus();
                this.clipboardWatcher.start();
                return;
            }

            const markInput = $$(this.NAMES.markInput);
            if (!markInput)
                return;

            const node = typeof markInput.getInputNode === "function"
                ? markInput.getInputNode()
                : null;
            this.scanner.start(node);
        }, 50);
    }

    _scanCaptureNode() {
        return document.getElementById("fmuScanCapture");
    }

    _setMobileToolbarVisible(visible) {
        if (!this.isMobile)
            return;

        const toolbar = this._findMainToolbar();
        if (!toolbar)
            return;

        const node = toolbar.$view;
        if (visible) {
            toolbar.define("height", 60);
            toolbar.show();
            if (node) {
                node.style.display = "";
                node.style.height = "";
                node.style.padding = "";
                node.style.overflow = "";
            }
        } else {
            toolbar.define("height", 0);
            toolbar.hide();
            if (node) {
                node.style.display = "none";
                node.style.height = "0px";
                node.style.padding = "0px";
                node.style.overflow = "hidden";
            }
        }

        toolbar.resize();
        toolbar.getParentView()?.resize();
        $$("root")?.resize();
    }

    _findMainToolbar() {
        if ($$("mainToolbar"))
            return $$("mainToolbar");

        let view = $$("toolbarLabel");
        while (view && view.config.view !== "toolbar")
            view = view.getParentView();

        return view;
    }

    _markInputEvents() {
        return {
            onEnter: () => this._onInputEnter(),
            onChange: (value) => this._onInputChange(value),
            onTimedKeyPress: () => {
                const node = this._markInputNode();
                const view = $$(this.NAMES.markInput);
                const value = node?.value ?? view?.getValue?.() ?? "";
                this._onInputChange(value);
            }
        };
    }

    _toDisplay(code) {
        return (code || "").replace(/\x1d/g, "\u241d");
    }

    _fromDisplay(value) {
        return (value || "").replace(/\u241d/g, "\x1d").replace(/<GS>/gi, "\x1d");
    }

    _escapeHtml(value) {
        return String(value)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    _markTemplate(code) {
        const display = this._toDisplay(code);
        if (!display)
            return `<span class="mark-scan-placeholder">${this._escapeHtml(this.LABELS.markPlaceholder)}</span>`;

        return this._escapeHtml(display);
    }

    _markInputNode() {
        if (this.isMobile)
            return this._scanCaptureNode();

        const view = $$(this.NAMES.markInput);
        if (!view || typeof view.getInputNode !== "function")
            return null;

        return view.getInputNode() || null;
    }

    _onClipboardMark(code) {
        if (this._checking)
            return;

        this._rawMark = code;
        this._scheduleAutoCheck();
    }

    _onBuffer(code) {
        this._rawMark = code || "";
        if (!this.isMobile)
            this._showMark(code);

        if (this._hasGsTail(this._rawMark))
            this._scheduleAutoCheck();
    }

    _hasGsTail(code) {
        const value = code || "";
        const gs = value.indexOf("\x1d");
        return gs >= 0 && value.length - gs - 1 >= 4;
    }

    _setCardsHtml(html) {
        const cards = $$(this.NAMES.markCards);
        if (!cards)
            return;

        cards.define("template", html);
        cards.refresh();
    }

    _showMark(code) {
        this._rawMark = code || "";
        const view = $$(this.NAMES.markInput);
        if (!view || typeof view.setValue !== "function")
            return;

        this._applyingScan = true;
        view.setValue(this._toDisplay(this._rawMark));
        this._applyingScan = false;
        this._scheduleIdleCheck();
    }

    _onInputChange(value) {
        if (this._applyingScan)
            return;

        this._rawMark = this._fromDisplay(value);
        this._scheduleIdleCheck();
    }

    _clearMarkInput() {
        if (this._autoCheckTimer) {
            clearTimeout(this._autoCheckTimer);
            this._autoCheckTimer = null;
        }

        this._rawMark = "";
        const node = this._markInputNode();
        this._applyingScan = true;
        if (node)
            node.value = "";
        const view = $$(this.NAMES.markInput);
        if (view)
            view.config.value = "";
        this._applyingScan = false;
    }

    _onInputEnter() {
        this._scheduleAutoCheck();
    }

    _currentMark() {
        if (this._rawMark)
            return this._rawMark;

        const node = this._markInputNode();
        if (node && node.value)
            return this._fromDisplay(node.value);

        const view = $$(this.NAMES.markInput);
        if (view && typeof view.getValue === "function")
            return this._fromDisplay(view.getValue() || "");

        return "";
    }

    _scheduleIdleCheck() {
        if (this._autoCheckTimer)
            clearTimeout(this._autoCheckTimer);

        this._autoCheckTimer = setTimeout(() => {
            const code = this._currentMark().trim();
            if (code.length >= 14)
                this._onCheck();
        }, this._idleCheckMs);
    }

    _scheduleAutoCheck() {
        if (this._autoCheckTimer)
            clearTimeout(this._autoCheckTimer);

        this._autoCheckTimer = setTimeout(() => {
            const code = this._currentMark().trim();
            if (code.length > 0)
                this._onCheck();
        }, 50);
    }

    _onScan(code, meta = {}) {
        this._rawMark = code || "";
        this.clipboardWatcher.remember(this._rawMark);
        if (!this.isMobile)
            this._showMark(code);

        this._scheduleAutoCheck();

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
        if (this._checking)
            return;

        const innInput = $$(this.NAMES.innInput);
        const markInput = $$(this.NAMES.markInput);
        const checkButton = $$(this.NAMES.checkButton);
        const form = $$(this.id);

        if (!this.isMobile && (!innInput || !markInput)) {
            webix.message({ text: "Ошибка: не найдены поля ввода", type: "error" });
            return;
        }

        let inn = (innInput?.getValue() || this.defaultInn || "").trim();
        if (!inn) {
            await this._loadInnFromConfig();
            inn = (innInput.getValue() || this.defaultInn || "").trim();
        }

        const markingCode = this._currentMark().trim();

        if (!inn) {
            webix.message({ text: "Введите ИНН организации", type: "error" });
            return;
        }

        if (!markingCode) {
            if (!this.isMobile)
                webix.message({ text: "Введите штрихкод маркировки", type: "error" });
            return;
        }

        this._checking = true;
        this.clipboardWatcher.remember(markingCode);
        this._setCardsHtml(buildCheckingHtml());
        if (!this.isMobile) {
            checkButton?.disable();
            webix.extend(form, webix.ProgressBar);
            form.showProgress({ type: "icon" });
        }

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
            this._checking = false;
            if (!this.isMobile) {
                checkButton?.enable();
                form.hideProgress();
            }
            this._clearMarkInput();
        }
    }

    _displayCombinedResponse(combined) {
        const cards = $$(this.NAMES.markCards);
        if (cards) {
            cards.define("template", buildMarkCardsHtml(combined));
            cards.refresh();
        }

        const jsonResponse = $$(this.NAMES.jsonResponse);
        if (jsonResponse) {
            jsonResponse.setValue(JSON.stringify(combined, null, 2));
        }
    }

    _toggleJson() {
        const jsonResponse = $$(this.NAMES.jsonResponse);
        const toggle = $$(this.NAMES.jsonToggle);
        if (!jsonResponse || !toggle)
            return;

        if (jsonResponse.isVisible()) {
            jsonResponse.hide();
            toggle.setValue(this.LABELS.showJson);
        } else {
            jsonResponse.show();
            toggle.setValue(this.LABELS.hideJson);
        }
    }

    _clearResponse() {
        const cards = $$(this.NAMES.markCards);
        if (cards) {
            cards.define("template", this.isMobile ? buildScanHintHtml() : buildMarkCardsHtml(null));
            cards.refresh();
        }

        const jsonResponse = $$(this.NAMES.jsonResponse);
        if (jsonResponse) {
            jsonResponse.setValue(this.LABELS.noResponse);
        }
    }
}

export default function (id) {
    const view = new MarkCheckView(id);
    view.render();
    view.delayedInnLoading();

    return view._formConfig;
}
