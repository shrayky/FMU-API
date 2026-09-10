/**
 * Захват ввода клавиатурного сканера (wedge) с сохранением спецсимволов (включая GS).
 * На ТСД Chrome часто даёт keydown Unidentified без символа — тогда берём keypress/input/textInput.
 */
export class ScannerWedge {
    /**
     * @param {{
     *   timeoutMs?: number,
     *   completeOnIdle?: boolean,
     *   gsAsEnterHoldMs?: number,
     *   onScan?: (code: string, meta: { capsLock: boolean, cyrillic: boolean }) => void,
     *   onBufferChange?: (code: string) => void,
     *   debug?: boolean
     * }} [options]
     */
    constructor(options = {}) {
        this.timeoutMs = options.timeoutMs ?? 50;
        this.completeOnIdle = options.completeOnIdle ?? false;
        this.gsAsEnterHoldMs = options.gsAsEnterHoldMs ?? 0;
        this.maxIdleWaitMs = options.maxIdleWaitMs ?? 800;
        /** @type {(code: string, meta: { capsLock: boolean, cyrillic: boolean }) => void} */
        this.onScan = options.onScan ?? null;
        /** @type {(code: string) => void} */
        this.onBufferChange = options.onBufferChange ?? null;
        this.debug = options.debug ?? false;
        this.buffer = [];
        this.timer = null;
        this._gsHoldTimer = null;
        this._idleWaitStarted = 0;
        this._inputNode = null;
        this._fromKeyDown = false;
        this.active = false;
        this.capsLock = false;
        this._onKeyDown = this._onKeyDown.bind(this);
        this._onKeyUp = this._onKeyUp.bind(this);
        this._onBeforeInput = this._onBeforeInput.bind(this);
        this._onInput = this._onInput.bind(this);
        this._onKeyPress = this._onKeyPress.bind(this);
        this._onPaste = this._onPaste.bind(this);
        this._onTextInput = this._onTextInput.bind(this);
        this._onCompositionEnd = this._onCompositionEnd.bind(this);
    }

    _log(...args) {
        if (this.debug) {
            console.log("[ScannerWedge]", ...args);
        }
    }

    _blockBrowser(e) {
        e.preventDefault();
        e.stopPropagation();
        e.stopImmediatePropagation();
    }

    /**
     * @param {HTMLElement} [inputNode] скрытое поле-ловушка для ТСД (input/beforeinput)
     */
    start(inputNode) {
        if (this.active) {
            this._bindInput(inputNode);
            this._log("start() — уже активен, обновлён input");
            return;
        }

        window.addEventListener("keydown", this._onKeyDown, true);
        window.addEventListener("keyup", this._onKeyUp, true);
        window.addEventListener("keypress", this._onKeyPress, true);
        if (this.completeOnIdle) {
            window.addEventListener("paste", this._onPaste, true);
            window.addEventListener("textInput", this._onTextInput, true);
            document.addEventListener("beforeinput", this._onBeforeInput, true);
            document.addEventListener("input", this._onInput, true);
            document.addEventListener("compositionend", this._onCompositionEnd, true);
        }
        this._bindInput(inputNode);
        this.active = true;
        this._log("start() — слушатели включены");
    }

    stop() {
        if (!this.active) {
            this._log("stop() пропущен — уже выключен");
            return;
        }

        window.removeEventListener("keydown", this._onKeyDown, true);
        window.removeEventListener("keyup", this._onKeyUp, true);
        window.removeEventListener("keypress", this._onKeyPress, true);
        window.removeEventListener("paste", this._onPaste, true);
        window.removeEventListener("textInput", this._onTextInput, true);
        document.removeEventListener("beforeinput", this._onBeforeInput, true);
        document.removeEventListener("input", this._onInput, true);
        document.removeEventListener("compositionend", this._onCompositionEnd, true);
        this._unbindInput();
        this._reset();
        this.active = false;
        this._log("stop() — слушатели выключены");
    }

    _bindInput(inputNode) {
        this._inputNode = inputNode || null;
    }

    _unbindInput() {
        this._inputNode = null;
    }

    _reset() {
        if (this.buffer.length > 0) {
            this._log("reset буфера, было символов:", this.buffer.length, JSON.stringify(this.buffer.join("")));
        }

        this.buffer = [];
        this.capsLock = false;
        this._idleWaitStarted = 0;

        if (this.timer) {
            clearTimeout(this.timer);
            this.timer = null;
        }

        if (this._gsHoldTimer) {
            clearTimeout(this._gsHoldTimer);
            this._gsHoldTimer = null;
        }
    }

    _finishScan() {
        const code = this.buffer.join("").trim();
        const meta = {
            capsLock: this.capsLock,
            cyrillic: this._hasCyrillic(code)
        };
        this._log("скан завершён:", JSON.stringify(code), meta);
        this._reset();

        if (this._inputNode)
            this._inputNode.value = "";

        if (code && typeof this.onScan === "function") {
            this.onScan(code, meta);
        }
    }

    _hasGsTail(code) {
        const gs = code.indexOf("\x1d");
        return gs >= 0 && code.length - gs - 1 >= 4;
    }

    _startGsHold() {
        if (this._gsHoldTimer) {
            clearTimeout(this._gsHoldTimer);
        }

        this._log("Enter без GS — ждём хвост", this.gsAsEnterHoldMs, "мс");
        this._gsHoldTimer = setTimeout(() => this._finishScan(), this.gsAsEnterHoldMs);
    }

    _hasCyrillic(code) {
        return /[а-яёА-ЯЁ]/.test(code);
    }

    _emitBuffer() {
        if (typeof this.onBufferChange === "function")
            this.onBufferChange(this.buffer.join(""));
    }

    _looksLikeCisPrefix(code) {
        return code.startsWith("01") && code.length >= 18 && code.indexOf("\x1d") < 0;
    }

    _hasIncompleteGs(code) {
        const gs = code.indexOf("\x1d");
        return gs >= 0 && code.length - gs - 1 < 4;
    }

    _scheduleReset() {
        if (this.timer) {
            clearTimeout(this.timer);
        }

        this.timer = setTimeout(() => {
            const code = this.buffer.join("");
            if (this.completeOnIdle && this._hasGsTail(code)) {
                this._log("таймаут", this.timeoutMs, "мс — марка с GS, завершаем скан");
                this._finishScan();
                return;
            }

            if (this.completeOnIdle && (this._looksLikeCisPrefix(code) || this._hasIncompleteGs(code))) {
                if (!this._idleWaitStarted)
                    this._idleWaitStarted = Date.now();

                if (Date.now() - this._idleWaitStarted < this.maxIdleWaitMs) {
                    this._log("таймаут — префикс КИ, ждём хвост после GS");
                    this._scheduleReset();
                    return;
                }
            }

            if (this.completeOnIdle && code.trim().length >= 14) {
                this._log("таймаут", this.timeoutMs, "мс — ввод закончен, завершаем скан");
                this._finishScan();
                return;
            }

            this._log("таймаут", this.timeoutMs, "мс — очистка буфера");
            this._reset();
        }, this.timeoutMs);
    }

    _keyToChar(e) {
        if (e.key === "Enter" || e.key === "\r") {
            return "\r";
        }

        if (e.key === "\x1d" || e.key === "F8" || e.key === "F12" ||
            (e.ctrlKey && !e.altKey && !e.metaKey && (e.key === "]" || e.code === "BracketRight"))) {
            return "\x1d";
        }

        if (e.key && e.key.length === 1 && !e.ctrlKey && !e.altKey && !e.metaKey) {
            return e.key;
        }

        const code = e.which || e.keyCode || 0;
        if (e.key === "Unidentified" || !e.key) {
            if (code === 13)
                return "\r";
            if (code === 29)
                return "\x1d";
            if (code >= 32 && code < 127)
                return String.fromCharCode(code);
        }

        return null;
    }

    _handleEnter() {
        if (this.buffer.length === 0)
            return;

        const current = this.buffer.join("");
        if (this.gsAsEnterHoldMs > 0 && current.indexOf("\x1d") < 0) {
            this._startGsHold();
            return;
        }

        this._finishScan();
    }

    /**
     * Добавляет символ скана в буфер (кроме CR — его обрабатывает _handleEnter).
     */
    _pushScanChar(ch) {
        if (ch === "\r" || ch === "\n") {
            this._handleEnter();
            return;
        }

        if (this._gsHoldTimer) {
            clearTimeout(this._gsHoldTimer);
            this._gsHoldTimer = null;
            if (this.buffer.join("").indexOf("\x1d") < 0) {
                this.buffer.push("\x1d");
                this._log("Enter был GS, в буфер {GS}");
            }
        }

        this.buffer.push(ch);
        this._idleWaitStarted = 0;
        this._log("в буфер:", ch === "\x1d" ? "{GS}" : ch, "bufferLen=", this.buffer.length);
        this._emitBuffer();
        this._scheduleReset();
    }

    /**
     * Кусок текста из input ТСД: префикс + хвост без GS склеиваются через GS.
     */
    _ingestChunk(chunk) {
        if (!chunk)
            return;

        chunk = String(chunk).replace(/\u241d/g, "\x1d").replace(/<GS>/gi, "\x1d");

        const current = this.buffer.join("");
        if (chunk === current)
            return;

        const printable = current.replace(/\x1d/g, "");
        const chunkPrintable = chunk.replace(/\x1d/g, "");
        if (chunkPrintable === printable)
            return;

        let extra = chunk;
        if (chunk.startsWith(current) && chunk.length > current.length)
            extra = chunk.slice(current.length);
        else if (chunk.startsWith(printable) && chunk.length > printable.length)
            extra = chunk.slice(printable.length);
        else if (chunkPrintable.startsWith(printable) && chunkPrintable.length > printable.length)
            extra = chunkPrintable.slice(printable.length);
        else if (printable && chunkPrintable.indexOf(printable) >= 0)
            extra = chunkPrintable.slice(chunkPrintable.indexOf(printable) + printable.length);

        if (this._looksLikeCisPrefix(current) && /^9[123]/.test(extra))
            this.buffer.push("\x1d");

        for (const ch of extra)
            this._pushScanChar(ch);
    }

    _isTypingTarget(target) {
        if (!target || !target.tagName)
            return false;

        const tag = target.tagName;
        return tag === "INPUT" || tag === "TEXTAREA" || target.isContentEditable;
    }

    _armInput() {
        if (!this._inputNode)
            return;

        if (document.activeElement !== this._inputNode)
            this._inputNode.focus();
    }

    _onBeforeInput(e) {
        if (e.inputType === "insertLineBreak" || e.inputType === "insertParagraph") {
            this._blockBrowser(e);
            this._handleEnter();
            return;
        }

        if (e.data)
            this._ingestChunk(e.data);
    }

    _onInput(e) {
        const target = e.target;
        const value = (target && target.value) || e.data || "";
        if (!value)
            return;

        this._ingestChunk(value);
    }

    _onPaste(e) {
        const text = (e.clipboardData || window.clipboardData)?.getData("text") || "";
        if (!text)
            return;

        this._ingestChunk(text);
        if (this.completeOnIdle || this.buffer.length > 0)
            this._blockBrowser(e);
    }

    _onTextInput(e) {
        if (e.data)
            this._ingestChunk(e.data);
    }

    _onCompositionEnd(e) {
        if (e.data)
            this._ingestChunk(e.data);
    }

    _onKeyPress(e) {
        this._armInput();
        const code = e.charCode || e.which || 0;
        if (code === 13) {
            this._blockBrowser(e);
            this._handleEnter();
            return;
        }

        if (code === 29) {
            this._blockBrowser(e);
            this._pushScanChar("\x1d");
            return;
        }

        if (this._fromKeyDown)
            return;

        if (code < 32 || code > 126)
            return;

        this._pushScanChar(String.fromCharCode(code));

        if (this.completeOnIdle && !this._isTypingTarget(e.target))
            this._blockBrowser(e);
    }

    _onKeyUp(e) {
        if (this._isTypingTarget(e.target))
            return;

        if (this.buffer.length > 0)
            this._blockBrowser(e);
    }

    _onKeyDown(e) {
        this._armInput();

        if (!this.capsLock && typeof e.getModifierState === "function") {
            this.capsLock = e.getModifierState("CapsLock");
        }

        const ch = this._keyToChar(e);
        this._fromKeyDown = ch !== null;
        const scanning = this.buffer.length > 0;
        const intoField = this._isTypingTarget(e.target);
        const inOtherField = intoField && e.target !== this._inputNode && !this.completeOnIdle;

        if (inOtherField)
            return;

        if (ch === "\r" || ch === "\x1d") {
            this._blockBrowser(e);
        } else if (ch !== null && scanning && !intoField) {
            this._blockBrowser(e);
        }

        this._log("keydown", {
            key: e.key,
            code: e.code,
            keyCode: e.keyCode,
            mapped: ch === "\x1d" ? "{GS}" : ch === "\r" ? "{CR}" : ch,
            bufferLen: this.buffer.length
        });

        if (ch === null)
            return;

        this._pushScanChar(ch);
    }
}
